#!/bin/bash

mkdir exp_logs

exp_idx=0
num_gpus=$(nvidia-smi --list-gpus | wc -l)

for file in ./generated/*.yaml; do
	 echo $file

	 exp_name=$(echo $file | sed "s/.yaml//")
	 exp_name=$(echo $exp_name | sed "s/.\/generated\///")

   ((exp_idx++))
   allocated_gpu=$(($exp_idx % $num_gpus))

	 nohup docker run --rm -t --name ttpcg_$exp_name -v $(pwd):/config -v $(pwd)/../Build/Linux:/game -v /mnt/nas/inchang/PuzzlePCG/ProxyPlayer:/workspace/results inchang/ct_game /bin/bash -c "chmod -R 755 /game && CUDA_VISIBLE_DEVICES=$allocated_gpu mlagents-learn /config/$file --env /game/Game.x86_64 --no-graphics --run-id $exp_name" > ./exp_logs/$exp_name.log 2>&1 &
   sleep 1
done
