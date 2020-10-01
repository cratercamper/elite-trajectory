#!/bin/bash

#
# convert-server-data-dir.sh
#
# Converts filenames, adds newlines to .json Elite:Dangerous Status.json data
#   (collected via SRVTracker.json)
#   NOTE: You have also manually change Race.StartRequest.json
#         - take only route from it (curly brackets following "Route":)
#


shopt -s nullglob

function convert_dir {
	if [ "$(file -b $1)" != "directory" ]; then return ; fi

	pushd $1 > /dev/null

	echo -e "\n\n==== Converting:$1 ====================="
	#convert names - spaces to _ and .json suffix
	for f in *.Tracking *.StartRequest *.Summary ; do
		filename="${f%.*}"    # remove 'Tracking' suffix
		filename=$f
		filename_nospace=${filename// /_}
		filename_nospace_json=$filename_nospace".json"
		echo "F:${filename_nospace_json}"

		mv -v "$f" $filename_nospace_json
	done


	for f in *.json ; do
		echo "filelines: $f"
		sed --in-place 's/},/},\n/g' $f
		sed --in-place '/^$/d' $f

	done


	popd > /dev/null
}



for dir in ./*; do
	echo $dir;
	convert_dir $dir
	done


