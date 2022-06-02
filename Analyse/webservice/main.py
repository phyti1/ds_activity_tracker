import pickle
import flask
import pandas as pd
import numpy as np
import os, io, json
from math import sin, cos, sqrt, atan2, radians

app = flask.Flask(__name__)
port = int(9099)

model_path = "./Analyse/webservice/models/"
if(not os.path.exists(model_path)):
    raise Exception("Model path does not exist")

models = dict()
for root, dirs, files in os.walk(model_path):
  for file in files:
      if file.endswith(".pkl"):
          model_name = file.split(".")[0]
          # print(model_name)
          models[model_name] = pickle.load(open(model_path + file, 'rb'))



@app.route('/predict', methods=['POST'])
def predict():
  try:
    # strict = False because of invalid control character in csv
    post_content = json.loads(flask.request.get_data(as_text=True), strict=False)
    csv_test = post_content["data"]
    buf = io.StringIO()
    buf.write(csv_test)
    # reset buffer read position
    buf.seek(0)
    df_test = pd.read_csv(buf, sep=',', header=None)

    df_test = prep_allgemein(df_test)
    
    if(post_content["model"] == "SPR_RandomForest"):
      df_test = process_data_group_bienli(df_test)
      # TODO Modell mit 25 features trainieren
      prediction = models['rf_allprop'].predict(df_test.iloc[0, 2:].values.reshape((1, -1)))

    if(post_content["model"] == "SPR_CNN1"):
      prediction = models['cnn_XYZ'].predict(df_test)

    if(post_content["model"] == "SPR_CNN2"):
      prediction = models['cnn2_XYZ'].predict(df_test)

    response = prediction[0]

    return response
  except Exception as e:
    return {'error': str(e)}


def prep_allgemein(df):
  df.columns = ['time','name','activity','acc_x','acc_y','acc_z','mag_x','mag_y','mag_z','gyr_x','gyr_y','gyr_z','ori_x','ori_y','ori_z','ori_w','lat','long']
  df.loc[:, 'time'] = pd.to_datetime(df.loc[:, 'time'] + "000") # format="%d.%m.%Y %H:%M.%S.%f"
  return df


def process_data_group_bienli(df):
                
  df['time'] = pd.to_datetime(df['time'])

  
  # accelometer
  df['acc'] = (df.iloc[:, 3:6] + 10).sum(axis=1)
  # magnetometer
  df['mag'] = (df.iloc[:, 6:9] + 2000).sum(axis=1)
  # gyroscope
  df['gyr'] = (df.iloc[:, 9:12] + 30).sum(axis=1)
  # orientation
  df['ori'] = (df.iloc[:, 12:16] + 1).sum(axis=1)

  # drop old axis columns
  df.drop(columns=df.columns[3:16], inplace=True)
  
  
  gps_diff = (df.loc[:, ["lat", "long"]] - df.loc[:, ["lat", "long"]].shift(-1))
  gps_diff.replace(0, np.NAN, inplace=True)
  gps_diff[abs(gps_diff) > 1e-3] = np.NAN
  
  df["gps_differs"] = (df[["lat", "long"]] - df[["lat", "long"]].shift(-1)).sum(axis=1).astype(bool)
  
  dists = []
  gps_differs_index = df.loc[df["gps_differs"] == True, "gps_differs"].index
  for i in range(gps_differs_index.shape[0]):
    dists.append(get_distance(df.loc[gps_differs_index[i], ["lat", "long"]], df.loc[gps_differs_index[i]+1, ["lat", "long"]]))
  
  # calculate time differences
  time_differences = pd.Series(df.loc[gps_differs_index, "time"].values - df.loc[gps_differs_index.insert(0, 0)[:-1], "time"].values)
  time_diff_secs = time_differences.dt.total_seconds()
  
  time_diff_secs.loc[(time_diff_secs > 10) | (time_diff_secs < 0)] = 0.0
  
  # m/s ausrechen, in km/h umrechnen
  kmh = (dists / time_diff_secs * 3.6)
  kmh[kmh == np.inf] = np.nan
  kmh[kmh > 120] = np.nan
  kmh[kmh < 1] = 0
  
  # geschwindigkeit inseriteren
  df.loc[gps_differs_index, "kmh"] = kmh
  df.drop(columns=["lat", "long", "gps_differs"], inplace=True)
  
  df["kmh"] = df["kmh"].ffill().fillna(0)
  
  old_time = pd.to_datetime("01.01.2022 00:00:00")

  df[f"5s_index0"] = ((df["time"] - old_time).dt.total_seconds() // 5)
  # for i  in range(1, 5):
  #   df[f"5s_index{i}"] = df[f"5s_index0"].shift(round(i * 90/5))

  # inplace sifted values at the beginning
  df.fillna(0, inplace=True)
  
  groups = ["5s_index0"] # ["5s_index", "name", "activity"]
  df_wind = df.groupby(groups)
  # print group count
  print(df_wind.size().count())
  
  target = df_wind[["name", "activity", "5s_index0"]].first().reset_index(drop=True)
  
  df_agg = df_wind[df.columns[3:8]].agg(["var", "mean", "median", "min", "max"]).reset_index()
  
  # flatten mutli index columns
  df_agg.columns = ['_'.join(column) for column in df_agg.columns]
  
  df_agg = df_agg.drop(columns=["5s_index0_"])
  return df_agg
                
                
def get_distance(point1, point2):
  if(point1[0] == point2[0] and point1[1] == point2[1]):
    return 0.0

  R = 6370
  lat1 = radians(point1[0])  #insert value
  lon1 = radians(point1[1])
  lat2 = radians(point2[0])
  lon2 = radians(point2[1])

  dlon = lon2 - lon1
  dlat = lat2- lat1

  a = sin(dlat / 2)**2 + cos(lat1) * cos(lat2) * sin(dlon / 2)**2
  c = 2 * atan2(sqrt(a), sqrt(1-a))
  distance = R * c
  return distance



if __name__ == '__main__':
    //host on every ip avaliable
    app.run(host="0.0.0.0", port=port)




