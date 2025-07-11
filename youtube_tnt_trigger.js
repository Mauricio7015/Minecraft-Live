const {google} = require('googleapis');
const API_KEY = 'YOUR_YOUTUBE_API_KEY';

function extractVideoId(url) {
  const match = url.match(/[?&]v=([\w-]{11})/);
  return match ? match[1] : null;
}

function triggerTnt() {
  console.log('TNT acionada!');
}

async function fetchLikeCount(videoId) {
  const youtube = google.youtube({version: 'v3', auth: API_KEY});
  const res = await youtube.videos.list({
    part: 'statistics',
    id: videoId,
  });
  const items = res.data.items || [];
  if (items.length === 0) {
    throw new Error('Vídeo não encontrado.');
  }
  return parseInt(items[0].statistics.likeCount || '0', 10);
}

async function monitorLikes(videoId) {
  let prevLikes = 0;
  while (true) {
    try {
      const likes = await fetchLikeCount(videoId);
      if (likes > prevLikes) {
        console.log(`Novos likes detectados: ${likes} (anterior ${prevLikes})`);
        triggerTnt();
        prevLikes = likes;
      }
    } catch (err) {
      console.error(err.message);
      return;
    }
    await new Promise(r => setTimeout(r, 10000));
  }
}

async function main() {
  const url = process.argv[2];
  if (!url) {
    console.log('Uso: node youtube_tnt_trigger.js <url_da_live>');
    process.exit(1);
  }
  const videoId = extractVideoId(url);
  if (!videoId) {
    console.log('URL da live inválida.');
    process.exit(1);
  }
  await monitorLikes(videoId);
}

main();
