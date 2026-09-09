// Keep reusable visual assets private. Flattened README gameplay captures are allowed.
const fs = require('node:fs');
const path = require('node:path');
const cp = require('node:child_process');
const root = path.resolve(__dirname, '..');
const privateRoots = [
  'Assets/AfterSignal/Resources/Art', 'Assets/AfterSignal/Resources/Characters',
  'Assets/AfterSignal/Resources/Creatures', 'Assets/AfterSignal/Resources/Geometry',
  'Assets/AfterSignal/Resources/Maritime', 'Assets/AfterSignal/Resources/Response',
  'Assets/AfterSignal/Resources/Security', 'Assets/AfterSignal/Resources/WorldAssets',
  'Assets/AfterSignal/Prefabs', 'Assets/AfterSignal/Scenes',
];
const visual = /\.(png|jpe?g|webp|gif|tga|tiff?|psd|exr|hdr|svg|fbx|blend\d*|obj|mtl|glb|gltf|stl|usd[acz]?|unitypackage)(\.meta)?$/i;
function isPrivate(name) {
  if (/^Documentation\/Screenshots\/[a-z0-9-]+\.(png|jpg)$/.test(name)) return false;
  return visual.test(name) || privateRoots.some(p => name === p || name === p+'.meta' || name.startsWith(p+'/'));
}
function git(args) { return cp.execFileSync('git', args, {cwd:root, encoding:'utf8', maxBuffer:32*1024*1024}); }
function tracked() { return git(['ls-files','-z']).split('\0').filter(Boolean); }
if (require.main === module) {
  const mode = process.argv[2];
  const blocked = tracked().filter(isPrivate);
  if (mode === '--untrack') {
    const backup=path.join(root,'Artifacts','PrivateAssets');fs.mkdirSync(backup,{recursive:true});
    const list=path.join(backup,'untracked-paths.nul');fs.writeFileSync(list,blocked.join('\0')+(blocked.length?'\0':''));
    if(blocked.length)git(['rm','--cached','--pathspec-from-file='+list,'--pathspec-file-nul']);
    console.log(`${blocked.length} private asset paths removed from the index; local files retained.`);
  } else {
    if(blocked.length){console.error(`Public asset check failed: ${blocked.length} reusable visual assets are tracked.\n`+blocked.slice(0,12).join('\n'));process.exitCode=1;}
    else console.log('Public asset check passed: no reusable 3D/image assets in the index.');
  }
}
module.exports={isPrivate,privateRoots};
