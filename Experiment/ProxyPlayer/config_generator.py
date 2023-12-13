import yaml
import os
import copy

ENV_ARGS = list()

MCTS_SIMULATION_TIMES = [100, 200, 400, 800]
# MCTS_SIMULATION_TIMES = [200]
SAMPLING_NUMS = [5, 15]
TARGET_PLAYER = list(range(0, 11))  # 11
METHOD = ['ga']
# METHOD = ['mcts', 'random']
OBJECTIVE = ['knowledge', 'score']
TARGET_EPISODE_COUNT = 500
INCLUDE_SIMPLE_EFFECT = [1]
KNOWLEDGE_ALMOST_RATIO = ['1.0']
GREEDY_RATIO = ['1.0']
PLAYER_DEPTH = [1]
GA_EVOLUTION = [2, 4, 8]
os.makedirs('generated', exist_ok=True)


with open('base_config.yaml') as f:
    base_config = yaml.load(f, Loader=yaml.FullLoader)

for simple_effect in INCLUDE_SIMPLE_EFFECT:

    for i_method in METHOD:
        for i_player in TARGET_PLAYER:
            run_id = f'met_{i_method}_pla_{i_player}'

            RUN_ID = list()
            RUN_ID.append('met'), RUN_ID.append(i_method)
            RUN_ID.append('pla'), RUN_ID.append(i_player)
            if simple_effect == 1:
                RUN_ID.append('eff'), RUN_ID.append(simple_effect)
                run_id += f'_eff_1'

            if i_method == 'mcts':

                for i_p_depth in PLAYER_DEPTH:

                    for i_objective in OBJECTIVE:

                        if i_objective == 'knowledge':

                            for i_ratio in KNOWLEDGE_ALMOST_RATIO:
                                for i_simulation in MCTS_SIMULATION_TIMES:

                                    for i_g_ratio in GREEDY_RATIO:

                                        _RUN_ID = RUN_ID.copy()
                                        _RUN_ID.append('obj'), _RUN_ID.append(i_objective)
                                        _RUN_ID.append('sim'), _RUN_ID.append(i_simulation)
                                        _RUN_ID.append('rat'), _RUN_ID.append(i_ratio)
                                        _RUN_ID.append('pdep'), _RUN_ID.append(i_p_depth)
                                        _RUN_ID.append('gp'), _RUN_ID.append(i_g_ratio)

                                        _run_id = '_'.join(map(str, _RUN_ID))

                                        ENV_ARGS = list()
                                        ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
                                        ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
                                        ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)
                                        if simple_effect == 1:
                                            ENV_ARGS.append('--simpleEffect')
                                        ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
                                        ENV_ARGS.append('--mctsSimulation'), ENV_ARGS.append(i_simulation)
                                        ENV_ARGS.append('--objective'), ENV_ARGS.append(i_objective)
                                        ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)
                                        ENV_ARGS.append('--knowledgeAlmostRatio'), ENV_ARGS.append(i_ratio)
                                        ENV_ARGS.append('--playerDepth'), ENV_ARGS.append(i_p_depth)
                                        ENV_ARGS.append('--greedyActionRatio'), ENV_ARGS.append(i_g_ratio)

                                        base_config['env_settings']['env_args'] = ENV_ARGS
                                        print(_RUN_ID)

                                        with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                                            yaml.dump(base_config, f)
                        else:

                            for i_simulation in MCTS_SIMULATION_TIMES:
                                _RUN_ID = RUN_ID.copy()
                                _RUN_ID.append('obj'), _RUN_ID.append(i_objective)
                                _RUN_ID.append('sim'), _RUN_ID.append(i_simulation)
                                _RUN_ID.append('pdep'), _RUN_ID.append(i_p_depth)

                                _run_id = '_'.join(map(str, _RUN_ID))

                                ENV_ARGS = list()
                                ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
                                ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
                                ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)
                                if simple_effect == 1:
                                    ENV_ARGS.append('--simpleEffect')
                                ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
                                ENV_ARGS.append('--mctsSimulation'), ENV_ARGS.append(i_simulation)
                                ENV_ARGS.append('--objective'), ENV_ARGS.append(i_objective)
                                ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)
                                ENV_ARGS.append('--playerDepth'), ENV_ARGS.append(i_p_depth)

                                base_config['env_settings']['env_args'] = ENV_ARGS
                                print(_RUN_ID)

                                with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                                    yaml.dump(base_config, f)

            elif i_method == 'sampling':
                for i_simulation in SAMPLING_NUMS:
                    _RUN_ID = RUN_ID.copy()
                    _RUN_ID.append('sim'), _RUN_ID.append(i_simulation)

                    _run_id = '_'.join(map(str, _RUN_ID))

                    ENV_ARGS = list()
                    ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
                    ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
                    ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)
                    if simple_effect == 1:
                        ENV_ARGS.append('--simpleEffect')
                    ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
                    ENV_ARGS.append('--samplingNum'), ENV_ARGS.append(i_simulation)
                    ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)

                    base_config['env_settings']['env_args'] = ENV_ARGS
                    print(_RUN_ID)

                    with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                        yaml.dump(base_config, f)

            elif i_method == 'ga':

                for i_objective in OBJECTIVE:
                    for i_evolution in GA_EVOLUTION:
                        _RUN_ID = RUN_ID.copy()
                        _RUN_ID.append('obj'), _RUN_ID.append(i_objective)
                        _RUN_ID.append('evo'), _RUN_ID.append(i_evolution)
                        _run_id = '_'.join(map(str, _RUN_ID))

                        ENV_ARGS = list()
                        ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
                        ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
                        ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)

                        if simple_effect == 1:
                            ENV_ARGS.append('--simpleEffect')
                        ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
                        ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)
                        ENV_ARGS.append('--objective'), ENV_ARGS.append(i_objective)
                        ENV_ARGS.append('--evolutionNum'), ENV_ARGS.append(i_evolution)

                        base_config['env_settings']['env_args'] = ENV_ARGS
                        print(_RUN_ID)

                        with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                            yaml.dump(base_config, f)

            else:
                ENV_ARGS = list()
                _run_id = run_id

                ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
                ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
                ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)
                if simple_effect == 1:
                    ENV_ARGS.append('--simpleEffect')
                ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
                ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)
                base_config['env_settings']['env_args'] = ENV_ARGS
                with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                    yaml.dump(base_config, f)
