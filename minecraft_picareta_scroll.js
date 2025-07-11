if (!localStorage.getItem('loggedInUser')) {
  window.location.href = 'login.html';
}

const { Engine, Render, Runner, World, Bodies, Events } = Matter;
const BLOCK_SIZE = 45;
const NUM_BLOCKS = 8;
const URL_LIVE = "https://www.youtube.com/live/MlOpWnivxro";
const API_KEY = "YOUR_YOUTUBE_API_KEY";
let currentPickaxeType = 'iron';

const engine = Engine.create();
const world = engine.world;
engine.world.gravity.y = 2;

const render = Render.create({
  element: document.body,
  engine: engine,
  options: {
    width: window.innerWidth,
    height: window.innerHeight,
    wireframes: false,
    background: 'transparent'
  }
});

Render.run(render);
const runner = Runner.create();
Runner.run(runner, engine);

const game = document.getElementById('game');
const pickaxeEl = document.getElementById('pickaxe');
pickaxeEl.style.backgroundImage = `url('tool/pickaxe_${currentPickaxeType}.png')`;
const pickaxe = Bodies.rectangle(window.innerWidth / 2, 100, 48, 48, {
  restitution: 0.5,
  friction: 0.3,
  density: 0.002,
  render: { visible: false }
});
World.add(world, pickaxe);

const blockHealthMap = {
  cobblestone: 1,
  diamond: 4,
  diamond_full: 7,
  gold: 3,
  gold_full: 6,
  iron: 2,
  iron_full: 5
};

const pickaxeSpeedLevels = {
  slow: 2,
  normal: 5,
  fast: 8
};
let currentSpeedLevel = 'fast';

const blocks = [];
const walls = [];
let level = 0;

const brokenCounts = {
  cobblestone: 0,
  diamond: 0,
  gold: 0,
  iron: 0
};

function getRandomBlockClass() {
  const rand = Math.random() * 100;
  if (rand < 1) return 'diamond_full';
  if (rand < 7) return 'diamond';
  if (rand < 12) return 'gold_full';
  if (rand < 22) return 'gold';
  if (rand < 30) return 'iron_full';
  if (rand < 50) return 'iron';
  return 'cobblestone';
}

function createBlockRow(y) {
  const startX = window.innerWidth / 2 - (NUM_BLOCKS * BLOCK_SIZE) / 2;

  const leftWall = Bodies.rectangle(startX - BLOCK_SIZE / 2, y + BLOCK_SIZE / 2, BLOCK_SIZE, BLOCK_SIZE, { isStatic: true });
  const rightWall = Bodies.rectangle(startX + NUM_BLOCKS * BLOCK_SIZE + BLOCK_SIZE / 2, y + BLOCK_SIZE / 2, BLOCK_SIZE, BLOCK_SIZE, { isStatic: true });
  World.add(world, [leftWall, rightWall]);
  walls.push(leftWall, rightWall);

  const leftWallEl = document.createElement('div');
  leftWallEl.className = 'block wall-block';
  leftWallEl.style.left = `${startX - BLOCK_SIZE}px`;
  leftWallEl.style.top = `${y}px`;
  leftWall._el = leftWallEl;
  game.appendChild(leftWallEl);

  const rightWallEl = document.createElement('div');
  rightWallEl.className = 'block wall-block';
  rightWallEl.style.left = `${startX + NUM_BLOCKS * BLOCK_SIZE}px`;
  rightWallEl.style.top = `${y}px`;
  rightWall._el = rightWallEl;
  game.appendChild(rightWallEl);

  for (let i = 0; i < NUM_BLOCKS; i++) {
    const x = startX + i * BLOCK_SIZE + BLOCK_SIZE / 2;
    const blockType = getRandomBlockClass();
    const block = Bodies.rectangle(x, y + BLOCK_SIZE / 2, BLOCK_SIZE, BLOCK_SIZE, { isStatic: true });
    block._hits = 0;
    block._type = blockType;
    block._health = blockHealthMap[blockType];

    const blockEl = document.createElement('div');
    blockEl.className = `block ${blockType}`;
    blockEl.style.left = `${x - BLOCK_SIZE / 2}px`;
    blockEl.style.top = `${y}px`;
    block._el = blockEl;

    World.add(world, block);
    game.appendChild(blockEl);
    blocks.push(block);
  }
}

function updateCounter() {
  document.getElementById('count-diamond').textContent = brokenCounts.diamond;
  document.getElementById('count-gold').textContent = brokenCounts.gold;
  document.getElementById('count-iron').textContent = brokenCounts.iron;
  document.getElementById('pickaxe-level').textContent = level;
}

Events.on(engine, 'collisionStart', event => {
  event.pairs.forEach(pair => {
    blocks.forEach((block, index) => {
      const otherBody = pair.bodyA === block ? pair.bodyB : pair.bodyA;

      if ((pair.bodyA === block || pair.bodyB === block) && otherBody === pickaxe) {
        block._hits++;
        if (block._hits >= block._health) {
          const baseType = block._type.replace('_full', '');
          const isFull = block._type.includes('_full');
          brokenCounts[baseType] += isFull ? 9 : 1;
          if (block._el) block._el.remove();
          World.remove(world, block);
          blocks.splice(index, 1);
          updateCounter();
        }
      }
    });
  });
});

let stationaryTime = 0;
const stationaryThreshold = 0.9;
const stationaryDelay = 1000;
let impulseApplied = false;

Events.on(engine, 'beforeUpdate', event => {
  const delta = event.delta || 16.666;
  const speed = Math.sqrt(pickaxe.velocity.x ** 2 + pickaxe.velocity.y ** 2);

  if (speed < stationaryThreshold) {
    stationaryTime += delta;
    if (stationaryTime >= stationaryDelay && !impulseApplied) {
      Matter.Body.setVelocity(pickaxe, {
        x: pickaxe.velocity.x,
        y: -pickaxeSpeedLevels[currentSpeedLevel]
      });
      impulseApplied = true;
      stationaryTime = 0;
    }
  } else {
    stationaryTime = 0;
    impulseApplied = false;
  }
});

Events.on(engine, 'afterUpdate', () => {
  pickaxeEl.style.left = `${pickaxe.position.x}px`;
  pickaxeEl.style.top = `${pickaxe.position.y}px`;
  pickaxeEl.style.transform = `translate(-50%, -50%) rotate(${pickaxe.angle}rad)`;
  window.scrollTo({ top: pickaxe.position.y - window.innerHeight / 2, behavior: 'auto' });
});

function removeBlocksAboveMargin() {
  const limitY = pickaxe.position.y - (BLOCK_SIZE * 10);

  for (let i = blocks.length - 1; i >= 0; i--) {
    if (blocks[i].position.y < limitY) {
      if (blocks[i]._el) blocks[i]._el.remove();
      World.remove(world, blocks[i]);
      blocks.splice(i, 1);
    }
  }

  for (let i = walls.length - 1; i >= 0; i--) {
    if (walls[i].position.y < limitY) {
      if (walls[i]._el) walls[i]._el.remove();
      World.remove(world, walls[i]);
      walls.splice(i, 1);
    }
  }
}

let lastY = 300;
function spawnBlocksIfNeeded() {
  if (pickaxe.position.y > lastY - 300) {
    for (let i = 0; i < 5; i++) {
      createBlockRow(lastY);
      lastY += BLOCK_SIZE;
      level++;
    }
  }

  removeBlocksAboveMargin();
  requestAnimationFrame(spawnBlocksIfNeeded);
}

function explodeTNT(tnt) {
  if (!tnt || !tnt.position) return;

  const explosionRadius = 200;
  const { x, y } = tnt.position;

  if (tnt._el) tnt._el.remove();
  World.remove(world, tnt);

  const explosionEl = document.createElement('div');
  explosionEl.className = 'explosion';
  explosionEl.style.left = `${x - 60}px`;
  explosionEl.style.top = `${y - 60}px`;
  game.appendChild(explosionEl);

  setTimeout(() => explosionEl.remove(), 600);

  const destroyedBlocks = [];

  for (let i = 0; i < blocks.length; i++) {
    const block = blocks[i];
    const dx = block.position.x - x;
    const dy = block.position.y - y;
    const distance = Math.sqrt(dx * dx + dy * dy);

    if (distance <= explosionRadius) {
      const baseType = block._type.replace('_full', '');
      const isFull = block._type.includes('_full');
      if (brokenCounts.hasOwnProperty(baseType)) {
        brokenCounts[baseType] += isFull ? 9 : 1;
      }

      destroyedBlocks.push(block);
    }
  }

  destroyedBlocks.forEach(block => {
    if (block._el) block._el.remove();
    World.remove(world, block);
    const index = blocks.indexOf(block);
    if (index !== -1) blocks.splice(index, 1);
  });

  updateCounter();
}

function insertTntBlockAbovePickaxe() {
  const startX = window.innerWidth / 2 - (NUM_BLOCKS * BLOCK_SIZE) / 2;
  const pickaxeX = pickaxe.position.x;
  const pickaxeY = pickaxe.position.y;

  let columnIndex = Math.floor((pickaxeX - startX) / BLOCK_SIZE);
  columnIndex = Math.max(0, Math.min(NUM_BLOCKS - 1, columnIndex));
  const x = startX + columnIndex * BLOCK_SIZE + BLOCK_SIZE / 2;

  const possibleYPositions = [];
  for (let y = pickaxeY - BLOCK_SIZE; y > pickaxeY - BLOCK_SIZE * 10; y -= BLOCK_SIZE) {
    const occupied = blocks.some(block => {
      return Math.abs(block.position.x - x) < 1 && Math.abs(block.position.y - (y + BLOCK_SIZE / 2)) < 1;
    });

    if (!occupied) {
      possibleYPositions.push(y + BLOCK_SIZE / 2);
    }
  }

  if (possibleYPositions.length === 0) return;

  const y = possibleYPositions[0];

  const tnt = Bodies.rectangle(x, y, BLOCK_SIZE, BLOCK_SIZE, {
    restitution: 0.5,
    friction: 0.3,
    density: 0.002
  });

  tnt._type = 'tnt';

  const tntEl = document.createElement('div');
  tntEl.className = 'block tnt blinking';
  tntEl.style.left = `${x - BLOCK_SIZE / 2}px`;
  tntEl.style.top = `${y - BLOCK_SIZE / 2}px`;
  tnt._el = tntEl;

  World.add(world, tnt);
  game.appendChild(tntEl);

  Events.on(engine, 'afterUpdate', () => {
    if (tnt._el) {
      tnt._el.style.left = `${tnt.position.x - BLOCK_SIZE / 2}px`;
      tnt._el.style.top = `${tnt.position.y - BLOCK_SIZE / 2}px`;
    }
  });

  setTimeout(() => {
    explodeTNT(tnt);
  }, 3000);
}

let likes = 0;
let processing = false;

async function processLikesBatch(count) {
  for (let i = 0; i < count; i++) {
    insertTntBlockAbovePickaxe();
  }
}

async function handleNewLikes(diff) {
  const batchSize = 5;
  const delayMs = 5000;

  processing = true;

  while (diff > 0) {
    const currentBatch = Math.min(batchSize, diff);
    processLikesBatch(currentBatch);
    diff -= currentBatch;

    if (diff > 0) {
      await new Promise(resolve => setTimeout(resolve, delayMs));
    }
  }

  processing = false;
}

function extractVideoId(url) {
  const match = url.match(/(?:v=|\/live\/|\.be\/)([\w-]{11})/);
  return match ? match[1] : null;
}

async function getLikes() {
  if (processing) return;

  const videoId = extractVideoId(URL_LIVE);
  if (!videoId) return;

  const apiUrl = `https://www.googleapis.com/youtube/v3/videos?part=statistics&id=${videoId}&key=${API_KEY}`;

  try {
    const response = await fetch(apiUrl);
    const data = await response.json();
    const items = data.items || [];
    if (items.length === 0) return;
    const newLikes = parseInt(items[0].statistics.likeCount || '0', 10);

    if (newLikes > likes) {
      const diff = newLikes - likes;
      likes = newLikes;
      await handleNewLikes(diff);
    }
  } catch (error) {
    console.error(error);
  }

  setTimeout(getLikes, 5000);
}

getLikes();

createBlockRow(300);
createBlockRow(345);
createBlockRow(390);
spawnBlocksIfNeeded();

document.getElementById('logoutBtn').addEventListener('click', () => {
  localStorage.removeItem('loggedInUser');
  window.location.href = 'login.html';
});
