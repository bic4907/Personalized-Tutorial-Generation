import json
from glob import glob
from pprint import pprint
from os import path

files = glob('*.json')

answer = dict()
for file in files:
    with open(file, 'r') as f:
        json_obj = json.load(f)


    answer[json_obj['levelName']] = dict()

    target_block = json_obj['levelName'].split('_')[0].capitalize()


    for move in json_obj['moveList']:
        answer[json_obj['levelName']][move['moveIndex']] = target_block in move['type']
        # answer[json_obj['levelName']][move['moveIndex'] + 1] = move['type']

with open(path.join('..', 'answer.json'), 'w') as f:
    json.dump(answer, f, indent=4)
pprint(answer)
