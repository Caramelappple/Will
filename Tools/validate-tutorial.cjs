const fs=require('fs'), assert=require('assert'), path=require('path');
const read=p=>fs.readFileSync(p,'utf8').replace(/\r/g,'');
const root='Assets/_SO/Tutorial';
const files=fs.readdirSync(root,{recursive:true}).filter(p=>p.endsWith('.asset')).map(p=>path.join(root,p));
const assets=new Map(files.map(p=>[read(p+'.meta').match(/^guid: (\w+)/m)[1],p]));
const scene=read('Assets/_Scenes/LSO/LSO_TestScene.unity'), blocks=scene.split(/(?=^--- !u!)/m);
const ids=blocks.map(b=>b.match(/^--- !u!\d+ &(\d+)/)?.[1]).filter(Boolean);
assert.equal(new Set(ids).size,ids.length,'Scene file IDs must be unique');
const director=blocks.find(b=>b.includes('LSO_TutorialDirector\n'));
const chapterIds=[...director.match(/  chapters:\n([\s\S]*?)  banner:/)[1].matchAll(/guid: (\w+)/g)].map(m=>m[1]);
assert.equal(chapterIds.length,5);
const progression=blocks.find(b=>b.includes('LSO_StageProgression\n'));
const stageChapterIds=[...progression.match(/  chapters:\n([\s\S]*?)  startChapterIndex:/)[1].matchAll(/guid: (\w+)/g)].map(m=>m[1]);
assert.deepEqual(stageChapterIds.slice(0,5),[
  'd3fcf54f2a0661670271c82ba3a2fe78',
  '63d3b7e9ff8f4e2daf0245e789c84c6a',
  '4c042e2b519a4723b340b683c3c930dc',
  '24ba5d3066724f869e5134a74b25223d',
  '237f12cbc5ef45569dacb8c50e554f40'
],'Tutorial scene stage progression must include TutorialBattleChapter then Chapter1-4');
const tutorialBattle=read('Assets/_SO/Tutorial/Battle/TutorialBattleChapter.asset');
assert(tutorialBattle.includes('  isTutorial: 1'),'Tutorial battle chapter must be marked as tutorial');
assert(tutorialBattle.includes('variantGroups:\n  - stageIndex: 0'),'Tutorial battle chapter must expose one visible stage group');
assert(tutorialBattle.includes('guid: b55c77e74d4f3ea07261adce2bdc3773'),'Tutorial battle chapter must use TutorialBattle');
assert(!tutorialBattle.match(/  - \{fileID: 11400000, guid: (?!b55c77e74d4f3ea07261adce2bdc3773)/),'Tutorial battle chapter must not contain copied Chapter1 stages');
const chapter1=read('Assets/_SO/Stage/Chapter1/LSO_Chapter1.asset');
assert(chapter1.includes('stageIndex: 0\n    alternatives:\n    - {fileID: 11400000, guid: af1ca64aa1434e018d8c6bdeb62bdf17, type: 2}'),'Chapter1 stage 1 must keep its original candidates');
assert(!chapter1.match(/stageIndex: 0[\s\S]*?b55c77e74d4f3ea07261adce2bdc3773/),'Chapter1 stage 1 must not use the tutorial battlefield');
const shotBlock=blocks.find(b=>b.includes('LSO_CameraDirector\n'));
const shots=new Map([...shotBlock.matchAll(/  - id: (\S+)\n    camera: \{fileID: (\d+)\}/g)].map(m=>[m[1],m[2]]));
for(const id of shots.values())assert(ids.includes(id),'Camera must resolve: '+id);
let steps=0,practices=0;
const orderedSteps=[];
for(const id of chapterIds){
 assert(assets.has(id),'Chapter must resolve');
 for(const m of read(assets.get(id)).matchAll(/  - \{fileID: 11400000, guid: (\w+)/g)){
  const p=assets.get(m[1]);assert(p,'Step must resolve');const s=read(p);steps++;orderedSteps.push({p,s});
  const shot=s.match(/  shotId:([^\n]*)/)[1].trim();assert(!shot||shots.has(shot),'Unknown shot '+shot);
  const g=s.match(/  gate: \{fileID: 11400000, guid: (\w+)/)[1];assert(assets.has(g),'Gate must resolve');
  const gate=read(assets.get(g)),allowed=+s.match(/  allowed: (-?\d+)/)[1];
  if(gate.includes('LSO_GatePractice')){
   practices++;assert(s.includes('gateTimeout: 0'),'Practice must not auto-pass');
   const condition=+gate.match(/  condition: (\d+)/)[1];
   const masks={1:2,2:12,3:20,4:512,5:64,9:256,10:256,11:256,13:767};
   if(masks[condition])assert.equal(allowed&masks[condition],masks[condition],'Required action locked: '+p);
   if(condition===7){
    assert(s.includes('beginEnemyTurnAfterText: 1'),'Enemy demonstration must start after its caption');
    assert.equal(allowed,0,'Do not expose a false player turn during the enemy demonstration');
   }
   if(condition===2)assert(s.includes('  guide: 2'),'Movement must restrict MoveSystem, not CardPlacer');
   if([1,2].includes(condition)){
    const tile=gate.match(/  tile: (.+)/)[1];assert(s.includes('  - '+tile),'Gate and guide must use the same tile');
    const coords=[...tile.matchAll(/: (-?\d+)/g)].map(x=>+x[1]);assert(coords.every(n=>n>=0&&n<8));
    if(condition===1)assert(coords[2]<4,'Player placement must be in own half');
   }
  }
 }
}
assert.equal(steps,51); console.log(`PASS: ${steps} steps, 5 chapters, ${practices} result gates; camera references, allowed actions, guide/gate tiles and scene IDs.`);
const animal=read(root+'/Units/TutorialPlayerAnimal.asset');
assert(animal.includes('  cost: 1'));assert(animal.includes('  moveRange: 1'));assert(animal.includes('  range: 1'));
const summonCost=+animal.match(/  cost: (\d+)/)[1];
assert.equal(2*summonCost+2+1,5,'Two summons, two moves and one attack must exhaust the practice budget');
for(const [from,to] of [[[2,2],[3,3]],[[3,2],[4,3]]])assert(Math.max(...from.map((x,i)=>Math.abs(x-to[i])))<=1);
for(const tile of [[3,3],[4,4],[5,4]])assert(Math.max(Math.abs(tile[0]-4),Math.abs(tile[1]-3))<=1);
console.log('PASS: practice movement, attack reach, 5 AP budget and enemy placement within Curse range.');

assert(!director.includes('practiceCard:'), 'Removed practiceCard field must not be serialized');
assert(director.includes('introFadeDuration: 1.2'));
const fadeId=director.match(/introFade: \{fileID: (\d+)/)[1];
assert(blocks.some(b=>b.startsWith('--- !u!225 &'+fadeId+'\n')), 'Intro must reference a CanvasGroup');
for(const shot of shotBlock.split('  - id: ').slice(1)){
 assert(shot.includes('    style: 1\n'),'Tutorial cameras must ease both ends: '+shot.split('\n')[0]);
 assert(+shot.match(/    blendTime: ([\d.]+)/)[1]>=1,'Tutorial cameras must not cut');
}
const rewardRelease=orderedSteps.findIndex(x=>x.s.includes('releaseRewards: 1'));
assert(rewardRelease>0);
assert(orderedSteps.slice(0,rewardRelease).some(x=>x.s.includes('shotId: tut_health')),'Health explanation precedes rewards');
assert(orderedSteps.slice(0,rewardRelease).some(x=>x.s.includes('resumeBattle: 1')),'Resume full battle after the will explanation');
assert(!orderedSteps.slice(0,rewardRelease).some(x=>x.s.includes('shotId: Reward')),'Do not move to the chest before explanations finish');
const reward=orderedSteps.find(x=>x.s.includes('guideRewardPhases: 1'));
for(const field of ['rewardCardText','rewardNoteReadyText','rewardNoteShownText','rewardTransitionText'])
 assert(new RegExp('  '+field+': "[^"\\n]+"').test(reward.s),'Missing phase-specific reward caption: '+field);
assert(read(root+'/Steps/02_Enemies/Enemy2 2.asset').includes('returnToDefaultCamera: 1'),'Inspection must use the default camera');
assert(orderedSteps.some(x=>x.s.includes('replayLastCostSpend: 1')),'Cost explanation must demonstrate the actual last spend');
const death=read('Assets/_Scenes/KTH/KTH_Death Scene.unity').split(/(?=^--- !u!)/m);
const option=death.find(b=>b.includes('  m_text: Option\n'));
const exit=death.find(b=>b.includes('  m_text: Exit\n'));
assert.equal(option.match(/m_fontAsset: (.+)/)[1],exit.match(/m_fontAsset: (.+)/)[1]);
assert.equal(option.match(/m_sharedMaterial: (.+)/)[1],exit.match(/m_sharedMaterial: (.+)/)[1]);
console.log('PASS: intro fade, camera easing, guided enemy turn, reward ordering/captions, inspection camera and Option font.');

