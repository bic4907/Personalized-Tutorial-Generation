

import json

import pandas as pd
from tqdm import tqdm


with open('tutoringpcg-default-rtdb-export.json', 'r') as f:
    obj = json.load(f)
items = obj['0804']

with open('answer.json', 'r') as f:
    quiz_answer = json.load(f)

# Read google_form.csv file to dataframe
google_form_df = pd.read_csv('google_form.csv')
google_form_df = google_form_df.rename(columns={
    '타임스탬프': 'Timestamp',
    'Please enter the [Survey Code] on the game screen.': 'User',
    'How often do you play games in a week?': 'GF_PlayFrequency',
    ' How long time did you played match-3 game?': 'GF_PlayTime',
    'I think the tutorial session is helpful to learn the matching skills.': 'GF_TutorialHelpful',
    'How long did it take you to learn the game\'s basic mechanics?': 'GF_BasicMechanicTime',
    'How long did it take you to learn the special matching skills.': 'GF_SpecialSkillTime',
    'The tutorial helped me better remember the matching skills.': 'GF_TutorialHelpRemember',
    'I think I learned all of matching skill through the tutorial session.': 'GF_LearnedAllSkill',
    'I think I was involved in game playing.': 'GF_InvolvedInGame',
    'I think newly descended block was no different from the regular Match-3 games.': 'GF_NewBlockSame',
    'The hint doesn’t show information I already know.': 'GF_HintUseless',
})
# Change the GF_PlayFrequency to numerical value
google_form_df['GF_PlayFrequency'] = google_form_df['GF_PlayFrequency'].map({
    'Less than 1 hour': 0,
    '1-5 hours': 1,
    '6-10 hours': 2,
    '11-20 hours': 3,
    'More than 20 hours': 4,
})
# Change the GF_PlayTime to numerical value
google_form_df['GF_PlayTime'] = google_form_df['GF_PlayTime'].map({
    'No experience': 'A',
    'Lower than 1 hour ( < 1 hour)': 'B',
    'Higher than 1 hour ( > 1 hour)': 'C',
})

user_ids = google_form_df['User'].values
user_ids = [user_id.split('_')[1] for user_id in user_ids]
google_form_df['User'] = user_ids
google_form_df.sort_values(by='Timestamp', ascending=False, inplace=True)
google_form_df = google_form_df.groupby('User').first().reset_index()

# google_form_df.drop(columns=['타임스탬프'], inplace=True)


cnt = 0

learning_dicts = list()
quiz_dicts = list()
summarize_dicts = list()


def conv_method(short):
    _conv = {'RD': 'Random', 'MCKN': 'MCTS_Knowledge', 'MCSC': 'MCTS_Score'}
    return _conv[short]


def get_replacement(prefix):
    # Change L to prefix
    return {f'{key}': f'{prefix}_{key}' for key in ['BombPiece', 'CrossPiece', 'HorizontalPiece', 'RainbowPiece', 'RocketPiece', 'VerticalPiece']}


def update_dict_keys(d, key_map):
    new_dict = {}

    for old_key, new_key in key_map.items():
        if old_key in d:
            new_dict[new_key] = d[old_key]
        else:
            raise KeyError(f"Key '{old_key}' not found in the dictionary.")

    for key, value in d.items():
        if key not in key_map:
            new_dict[key] = value

    return new_dict


for key in tqdm(items):
    learning_dict, quiz_dict = dict(), dict()
    user = key.split('_')[1]
    method = conv_method(key.split('_')[0])

    item = items[key]

    _learning_list = list()
    learning_data = item['Learning']
    for data in learning_data.values():
        _learning_dict = learning_dict.copy()
        _learning_dict['User'] = user
        _learning_dict['Method'] = method
        _learning_dict['L_Step'] = data['TotalStepCount']
        _learning_dict['L_DecisionTime'] = data['DecisionTime']
        _learning_dict['PCGTime'] = data['PCGTime']
        _learning_dict['Time'] = data['Time']

        _learning_dict.update(data['CurrentLearned'])
        _learning_dict = update_dict_keys(_learning_dict, get_replacement('L'))
        _learning_dict.update(data['CurrentMatches'])
        _learning_dict = update_dict_keys(_learning_dict, get_replacement('CM'))
        _learning_list.append(_learning_dict)

        _learning_dict['EVT_MatchNoHint'] = False
        _learning_dict['EVT_MatchIgnoringHint'] = False

        if 'MatchEvent' in data.keys():
            _learning_dict['EVT_MatchNoHint'] = 'MatchNoHint' in data['MatchEvent']
            _learning_dict['EVT_MatchIgnoringHint'] = 'MatchIgnoringHint' in data['MatchEvent']

    if 'Quiz' not in item.keys():
        continue

    if len(item['Quiz']) != 12:
        continue

    _quiz_list = list()
    quiz_data = item['Quiz']
    for data in quiz_data.values():
        _quiz_dict = quiz_dict.copy()
        _quiz_dict['User'] = user
        _quiz_dict['Method'] = method
        _quiz_dict.update(data)

        def is_correct(level, action):
            action = str(action)
            if action not in quiz_answer[level]: return False
            return quiz_answer[level][str(action)]

        _quiz_dict['Correct'] = is_correct(_quiz_dict['QuizFile'], data['PlayerAction'])
        _quiz_dict['Time'] = data['Time']
        _quiz_list.append(_quiz_dict)

    learning_dicts += _learning_list
    quiz_dicts += _quiz_list

learning_df = pd.DataFrame(learning_dicts)
quiz_df = pd.DataFrame(quiz_dicts)


learning_df = learning_df[learning_df['User'].isin(user_ids)]
quiz_df = quiz_df[quiz_df['User'].isin(user_ids)]


learning_df = learning_df.rename(columns=get_replacement('L'))

quiz_df.to_csv('quiz_raw.csv')

quiz_df = quiz_df.copy()
quiz_df['TargetBlock'] = quiz_df['QuizFile'].map(lambda x: x.split('_')[0])
quiz_df = quiz_df.groupby(['User', 'Method', 'TargetBlock'])['User', 'Method', 'TargetBlock', 'Correct', 'DecisionTime'].agg({
    'Correct': "sum",
}).reset_index()
quiz_df['Correct'] = quiz_df['Correct'].apply(lambda x: min(x, 1))
# Place the TargetBlock values to the columns
quiz_df = quiz_df.pivot(index=['User'], columns='TargetBlock', values='Correct').reset_index()

# Change "bomb" to Q_BombPiece and apply for the all other pieces
quiz_df = quiz_df.rename(columns={'bomb': 'Q_BombPiece', 'cross': 'Q_CrossPiece', 'horizon': 'Q_HorizontalPiece',
                                  'rainbow': 'Q_RainbowPiece', 'rocket': 'Q_RocketPiece', 'vertical': 'Q_VerticalPiece'})


# Change the Q_* columns to boolean
quiz_df = quiz_df.copy()
for col in ['Q_BombPiece', 'Q_CrossPiece', 'Q_HorizontalPiece', 'Q_RainbowPiece', 'Q_RocketPiece', 'Q_VerticalPiece']:
    quiz_df[col] = quiz_df[col].apply(lambda x: True if x > 0 else False)


google_form_df = google_form_df[google_form_df['User'].isin(quiz_df['User'].values)]

values = google_form_df.groupby(['User']).count().reset_index()
print(values['User'])

# Merge the learnig_df and quiz_df
all_df = learning_df.merge(quiz_df, on=['User'], how='left')
print(len(all_df))
all_df = all_df.merge(google_form_df, on=['User'], how='left')
print(len(all_df))

print(len(set(all_df['User'])))

def get_match_event(row, col):
    if row['L_Step'] == 1: return 0

    comp_row = all_df[(all_df['User'] == row['User']) & (all_df['L_Step'] == row['L_Step'] - 1)]

    assert comp_row is not None

    diff = row[col] - comp_row[col]

    return int(diff.values[0])


for column in ['CM_BombPiece', 'CM_CrossPiece', 'CM_HorizontalPiece', 'CM_RainbowPiece', 'CM_RocketPiece', 'CM_VerticalPiece']:
   all_df['Diff_' + column] = all_df.apply(lambda row: get_match_event(row, column), axis=1)

for column in ['BombPiece', 'CrossPiece', 'HorizontalPiece', 'RainbowPiece', 'RocketPiece', 'VerticalPiece']:
    for match_event in ['MatchNoHint', 'MatchIgnoringHint']:
        all_df[f'EVT_{match_event}_{column}'] = all_df.apply(lambda row: (row[f'Diff_CM_{column}']) * (row[f'EVT_{match_event}']), axis=1)

# all_df['Diff_' + column] = all_df.apply(lambda row: get_match_event(row, column), axis=1)


all_df.to_csv('all_data.csv')
learning_df.to_csv('learning.csv')
quiz_df.to_csv('quiz.csv')



