import re
import sys
import time
from typing import Optional

from googleapiclient.discovery import build

API_KEY = "YOUR_YOUTUBE_API_KEY"


def extract_video_id(url: str) -> Optional[str]:
    """Extract the video ID from a YouTube URL."""
    match = re.search(r"v=([\w-]{11})", url)
    if match:
        return match.group(1)
    return None


def trigger_tnt() -> None:
    """Placeholder for TNT activation."""
    print("TNT acionada!")


def monitor_likes(video_id: str) -> None:
    youtube = build("youtube", "v3", developerKey=API_KEY)
    prev_likes = 0
    while True:
        req = youtube.videos().list(part="statistics", id=video_id)
        resp = req.execute()
        items = resp.get("items", [])
        if not items:
            print("Vídeo não encontrado.")
            return
        stats = items[0]["statistics"]
        likes = int(stats.get("likeCount", 0))
        if likes > prev_likes:
            print(f"Novos likes detectados: {likes} (anterior {prev_likes})")
            trigger_tnt()
            prev_likes = likes
        time.sleep(10)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Uso: python youtube_tnt_trigger.py <url_da_live>")
        sys.exit(1)
    url = sys.argv[1]
    video_id = extract_video_id(url)
    if not video_id:
        print("URL da live inválida.")
        sys.exit(1)
    monitor_likes(video_id)

