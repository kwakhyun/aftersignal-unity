using UnityEngine;

namespace AfterSignal
{
    public sealed class VehicleWheel : MonoBehaviour
    {
        public CityVehicle car;
        Quaternion rest;
        Vector3 axle, up, previous;
        float spin;
        bool front;
        public Vector3 WorldAxle => transform.parent.TransformDirection(axle);
        public void Initialize(CityVehicle owner)
        {
            car = owner; rest = transform.localRotation; previous = car.transform.position;
            axle = transform.parent.InverseTransformDirection(car.transform.forward).normalized;
            up = transform.parent.InverseTransformDirection(car.transform.up).normalized;
            front = car.transform.InverseTransformPoint(transform.position).x > 0;
        }
        void LateUpdate()
        {
            if (!car || car.Wrecked) return;
            float travel = Vector3.Dot(car.transform.position - previous, car.Forward);
            previous = car.transform.position;
            spin = Mathf.Repeat(spin - travel * 130, 360);
            var steering = Quaternion.AngleAxis(front ? car.Steering * 24 : 0, up);
            transform.localRotation = steering * Quaternion.AngleAxis(spin, axle) * rest;
        }
    }
}
