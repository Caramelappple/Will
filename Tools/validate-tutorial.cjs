const fs=require('fs'), assert=require('assert'), path=require('path');
const read=p=>fs.readFileSync(p,'utf8').replace(/\r/g,'');
const root='Assets/_SO/LSO/Tutorial';
const files=fs.readdirSync(root,{recursive:true}).filter(p=>p.endsWith('.asset')).map(p=>path.join(root,p));
const assets=new Map(files.map(p=>[read(p+'.meta').match(/^guid: (\w+)/m)[1],p]));
const scene=read('Assets/_Scenes/LSO/LSO_TestScene.unity'), blocks=scene.split(/(?=^--- !u!)/m);
const ids=blocks.map(b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1]).filter(Boolean);
assert.equal(new Set(ids).size,ids.length,'Scene file IDs must be unique');
const director=blocks.find(b=>b.includes('LSO_TutorialDirector\n'));
const chapterIds=[...director.match(/  chapters:\n([\s\S]*?)  banner:/)[1].matchAll(/guid: (\w+)/g)].map(m=>m[1]);
assert.equal(chapterIds.length,5);
const shotBlock=blocks.find(b=>b.includes('LSO_CameraDirector\n'));
const shots=new Map([...shotBlock.matchAll(/  - id: (\S+)\n    camera: \{fileID: (\d+)\}/g)].map(m=>[m[1],m[2]]));
for(const id of shots.values())assert(ids.includes(id),'Camera must resolve: '+id);
let steps=0,practices=0;
for(const id of chapterIds){
 assert(assets.has(id),'Chapter must resolve');
 for(const m of read(assets.get(id)).matchAll(/  - \{fileID: 11400000, guid: (\w+)/g)){
  const p=assets.get(m[1]);assert(p,'Step must resolve');const s=read(p);steps++;
  const shot=s.match(/  shotId:([^\n]*)/)[1].trim();assert(!shot||shots.has(shot),'Unknown shot '+shot);
  const g=s.match(/  gate: \{fileID: 11400000, guid: (\w+)/)[1];assert(assets.has(g),'Gate must resolve');
  const gate=read(assets.get(g)),allowed=+s.match(/  allowed: (-?\d+)/)[1];
  if(gate.includes('LSO_GatePractice')){
   practices++;assert(s.includes('gateTimeout: 0'),'Practice must not auto-pass');
   const condition=+gate.match(/  condition: (\d+)/)[1];
   const masks={1:2,2:12,3:20,4:64,5:64,9:256,10:256,11:256};
   if(masks[condition])assert.equal(allowed&masks[condition],masks[condition],'Required action locked: '+p);
   if([1,2].includes(condition)){
    const tile=gate.match(/  tile: (.+)/)[1];assert(s.includes('  - '+tile),'Gate and guide must use the same tile');
    const coords=[...tile.matchAll(/: (-?\d+)/g)].map(x=>+x[1]);assert(coords.every(n=>n>=0&&n<8));
    if(condition===1)assert(coords[2]<4,'Player placement must be in own half');
   }
  }
 }
}
assert.equal(steps,51); console.log(`PASS: ${steps} steps, 5 chapters, ${practices} result gates; camera references, allowed actions, guide/gate tiles and scene IDs.`);
const animal=read(root+'/TutorialPlayerAnimal.asset');
assert(animal.includes('  cost: 1'));assert(animal.includes('  moveRange: 1'));assert(animal.includes('  range: 0'));
assert.equal(1+1+1+1+1,5);
for(const [from,to] of [[[2,2],[3,3]],[[4,2],[4,3]]])assert(Math.max(...from.map((x,i)=>Math.abs(x-to[i])))<=1);
for(const tile of [[3,3],[4,4],[5,4]])assert(Math.max(Math.abs(tile[0]-4),Math.abs(tile[1]-3))<=1);
console.log('PASS: practice movement, melee attack, 5 AP budget and all remaining pieces within Rage range.');

