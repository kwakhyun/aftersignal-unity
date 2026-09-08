using UnityEngine;
namespace AfterSignal
{
    public sealed class PlayerEquipment:MonoBehaviour
    {
        PlayerMotor player;float cooldown,reload;
        public int Slot {get;private set;}
        public int ItemId=>ArmoryInventory.Equipped(Slot);
        public ArmoryItem Item=>ArmoryInventory.Items[Mathf.Max(0,ItemId)];
        public bool Extended=>Slot>=3;
        public bool Reloading=>reload>0;
        public float ReloadProgress=>1-reload/ReloadDuration;
        float ReloadDuration=>Slot==5?2.4f:Slot==6?2.0f:1.7f;
        public string Readout=>Reloading?"장전 중  "+(ReloadProgress*100).ToString("0")+"%":Slot==4?"수류탄 "+ArmoryInventory.Rounds(ItemId)+"개 · LMB 투척":ArmoryInventory.Rounds(ItemId)+" / "+ArmoryInventory.Reserve(ItemId)+"   R 장전";
        public int Shots {get;private set;}
        public void Initialize(PlayerMotor owner){player=owner;if(!GetComponent<HeldArmory>())gameObject.AddComponent<HeldArmory>();}
        public bool Select(int slot)
        {
            int id=ArmoryInventory.Equipped(slot);if(id<0||!ArmoryInventory.Owns(id)){player.Director.Toast("무기 상점에서 먼저 구매하세요.",1.4f);return false;}
            Slot=slot;reload=0;cooldown=.12f;player.EquipmentPose((WeaponId)Mathf.Min(slot,2));player.Director.Audio.Play("weapon_switch",player.Shoulder,.22f,1);return true;
        }
        public void Tick(ControlFrame input,float dt)
        {
            cooldown=Mathf.Max(0,cooldown-dt);
            if(input.weapon>=0&&input.weapon<7)Select(input.weapon);
            if(reload>0){reload=Mathf.Max(0,reload-dt);if(reload<=0){ArmoryInventory.Reload(ItemId);player.Director.Audio.Play("reload_slide",player.Shoulder,.25f,1);}}
            if(Extended&&input.reload)BeginReload();
        }
        public void BeginReload()
        {
            if(!Extended||Slot==4||Reloading||ArmoryInventory.Rounds(ItemId)>=Item.magazine||ArmoryInventory.Reserve(ItemId)<=0)return;
            reload=ReloadDuration;player.Director.Audio.Play("reload_out",player.Shoulder,.25f,1);
        }
        public void Attack()
        {
            if(!Extended||Reloading||cooldown>0)return;
            if(!ArmoryInventory.Consume(ItemId)){BeginReload();if(!Reloading){cooldown=.7f;player.Director.Toast("탄약이 없습니다. 무기 상점에서 보충하세요.",1.5f);}return;}
            cooldown=Item.interval;Shots++;player.EquipmentAttackPose(Mathf.Min(.35f,Item.interval));
            var direction=(player.Aim-player.Muzzle).normalized;
            if(Slot==4||Slot==5)
            {
                CombatProjectile.Launch(player,Slot==4,Item.damage);player.Director.Audio.Play(Slot==4?"blade_swing":"heavy_slam",player.Muzzle,Slot==4?.18f:.4f,2);return;
            }
            int pellets=Slot==6?7:1;
            var people=new System.Collections.Generic.Dictionary<WorldActor,float>();
            var enemies=new System.Collections.Generic.Dictionary<EnemyBrain,float>();
            var vehicles=new System.Collections.Generic.Dictionary<CityVehicle,float>();
            for(int i=0;i<pellets;i++)
            {
                var d=direction;if(pellets>1){var spread=Random.insideUnitCircle*.045f;d=(direction+player.Director.CameraRig.ViewRight*spread.x+Vector3.up*spread.y).normalized;}
                Vector3 end=player.Muzzle+d*Ballistics.Range;
                if(Ballistics.Cast(player.Muzzle,d,Ballistics.Range,player.transform,out var hit))
                {
                    end=hit.point;float damage=Item.damage*(Slot==6?Mathf.Lerp(1,.2f,Mathf.Clamp01(hit.distance/55)):1);
                    var person=hit.collider.GetComponentInParent<WorldActor>();if(person){people.TryGetValue(person,out var total);people[person]=total+damage;}
                    var enemy=hit.collider.GetComponentInParent<EnemyBrain>();if(enemy){enemies.TryGetValue(enemy,out var total);enemies[enemy]=total+damage;}
                    var car=hit.collider.GetComponentInParent<CityVehicle>();if(car){vehicles.TryGetValue(car,out var total);vehicles[car]=total+damage;}
                    hit.collider.GetComponentInParent<BreakableGlass>()?.Hit(damage);
                    hit.collider.GetComponentInParent<FacadeGlass>()?.Hit(hit.point,damage);
                    SignalEffects.Impact(end,-d,SignalEffects.Gold,.25f);
                }
                SignalEffects.Beam(player.Muzzle,end,SignalEffects.Gold,.018f,.06f);
            }
            foreach(var hit in people)hit.Key.Damage(hit.Value,direction*4);
            foreach(var hit in enemies)hit.Key.Damage(hit.Value,direction*3);
            foreach(var hit in vehicles)hit.Key.Damage(hit.Value,hit.Key.transform.position);
            player.Director.Audio.PlayGun(Slot==6?GunshotKind.Shotgun:GunshotKind.Rifle,player.Muzzle,.9f);
            player.Director.CameraRig.Kick(Slot==6?.06f:.022f);
        }
    }
    public sealed partial class PlayerMotor
    {
        public PlayerEquipment Equipment {get;private set;}
        public string EquippedName=>Equipment?Equipment.Item.name:Weapon.ToString();
        public void EquipmentPose(WeaponId weapon){CancelReload();Weapon=weapon;resolving=false;AttackTime=0;Combo=0;comboWindow=0;}
        public void EquipmentAttackPose(float duration){Attacks++;AttackDuration=AttackTime=duration;attackCooldown=duration;Action=WeaponAction.Combo;resolving=false;}
    }
}
