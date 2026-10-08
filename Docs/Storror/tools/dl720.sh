#!/bin/bash
# Download only the needed 720p segments (padded 1 s each side) — raw video stays on the box.
cd /workspace/storror
python3 -c "
import json
for c in json.load(open('clips.json')): print(c['id'], c['vid'], max(0,c['t0']-1), c['t1']+1)
" | while read id vid a b; do
  [ -s raw/$id.mp4 ] && { echo "skip $id"; continue; }
  venv/bin/yt-dlp -q --no-warnings --js-runtimes node -f "bv*[height<=720][vcodec^=avc1]/bv*[height<=720]" \
     --download-sections "*$a-$b" --force-keyframes-at-cuts -o "raw/$id.%(ext)s" "https://www.youtube.com/watch?v=$vid" </dev/null \
     && echo "{\"segment_start_s\": $a}" > raw/$id.json && echo "ok $id $(ffprobe -v error -select_streams v:0 -show_entries stream=width,height,r_frame_rate,duration -of csv=p=0 raw/$id.mp4 )" || echo "FAIL $id"
  sleep 60
done
