#!/bin/bash

mkdir exp_logs

exp_idx=0
num_gpus=$(nvidia-smi --list-gpus | wc -l)
num_cpus=$(nproc)
container_prefix="ttpcg"
max_container_count=$((num_cpus * 1/2))
file_list=()

directory="./generated"
for file in ./generated/*.yaml; do
	 file_list+=($file)
done
# Iterate over files in the directory
echo [$(date)] "Read files ("${#file_list[@]}"):"

while :
do
    container_count=$(docker ps -a --format '{{.Names}}' | grep -c "^$container_prefix")


    echo [$(date)] "Count of container ($container_prefix): " $container_count"/"$max_container_count
    echo [$(date)] "Count of remained tasks: " ${#file_list[@]}

    if [ $container_count -lt $max_container_count ]
    then
        random_index=$((RANDOM % ${#file_list[@]}))
        random_file=${file_list[$random_index]}


        # Print the randomly sampled file
        echo [$(date)] "Starting: $random_file"
        exp_name=$(echo $random_file | sed "s/.yaml//")
        exp_name=$(echo $exp_name | sed "s/.\/generated\///")

        if [ "$1" ]; then
            param_num_envs="--num-envs $1"
        else
            param_num_envs=""
        fi

        nohup docker run --rm -t --name $container_prefix"_"$exp_name -v $(pwd):/config -v $(pwd)/../Build/Linux:/game -v /mnt/nas/inchang/PuzzlePCG/ProxyPlayer:/workspace/results inchang/ct_game /bin/bash -c "chmod -R 755 /game && CUDA_VISIBLE_DEVICES=$allocated_gpu mlagents-learn /config/$random_file --env /game/Game.x86_64 --no-graphics --run-id $exp_name $param_num_envs" > ./exp_logs/$exp_name.log 2>&1 &

        unset "file_list[$random_index]"
        file_list=("${file_list[@]}")

        echo [$(date)] "Finished: $random_file"
    else
        echo [$(date)] "Waiting for container pool"

    fi
    sleep 1
done
