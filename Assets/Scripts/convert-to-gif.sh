#!/bin/bash

#
# convert-to-gif.sh
#
# Transforms .png screenshots into .gif
#
# ...screenshots taken every 5s, delay 50 creates .gif that is ~20 times faster than real time 
#    (see code of elite-trajectory (/Assets/Scripts/Screenshot.cs and /Assets/Scripts/mainControl.cs))
# ...requires imagemagick (convert, mogrify)
# ...files divided into batches, convert/mogrify fails when doing it all at once



WORK_DIR=/tmp/w/elite-trajectory/screenshots
DELAY=50
LOOP=1

#0 ...hi quality, 10+ ...blurred but small .gif
QUALITY=6


#mogrify -resize 1280x720 -format jpg $WORK_DIR/*.png

mogrify -format jpg $WORK_DIR/*.png

convert -delay $DELAY -loop $LOOP $WORK_DIR/00*.jpg $WORK_DIR/00.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/01*.jpg $WORK_DIR/01.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/02*.jpg $WORK_DIR/02.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/03*.jpg $WORK_DIR/03.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/04*.jpg $WORK_DIR/04.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/05*.jpg $WORK_DIR/05.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/06*.jpg $WORK_DIR/06.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/07*.jpg $WORK_DIR/07.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/08*.jpg $WORK_DIR/08.gif
convert -delay $DELAY -loop $LOOP $WORK_DIR/09*.jpg $WORK_DIR/09.gif

mogrify -layers 'optimize' -fuzz $QUALITY% $WORK_DIR/*.gif

convert $WORK_DIR/*.gif $WORK_DIR/out-final.gif
