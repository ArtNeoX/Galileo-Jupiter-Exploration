Launch scene bitmaps. All are shown full screen (scaled to cover the screen, like the office renders),
so paint them at the same size (e.g. 1920x1080) with transparency around the shuttle.

  background_good.png      launch pad, clear weather (12 and 18 October)
  background_bad.png       launch pad, bad weather (17 October, after the attempt)
  shuttle.png              shuttle, engines off
  shuttle_afterburner.png  shuttle with engines firing (18 October, successful launch)

Any file that is missing is replaced by a simple placeholder.
The red engine-failure cross is placed at launch.crossX / crossY in content.json
(0..1 across the shuttle bitmap, 0 = left/bottom).
