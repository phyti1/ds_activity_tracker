import pickle
import flask
import pandas as pd
import numpy as np
import os, io, json

app = flask.Flask(__name__)
port = int(9099)

model_path = "./Analyse/webservice/models/"

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
    # TODO data transformation used in preprocessing
    df_text = df_test.iloc[:180, :]
    
    if(post_content["model"] == "SPR_RandomForest"):
      prediction = models['rf_allprop'].predict(np.array([df_test[0].values]))

    if(post_content["model"] == "SPR_CNN1"):
      prediction = models['cnn_XYZ'].predict(np.array([df_test[0].values]))

    if(post_content["model"] == "SPR_CNN2"):
      prediction = models['cnn2_XYZ'].predict(np.array([df_test[0].values]))

    response = {'prediction': round(prediction[0], 2)}

    return response
  except Exception as e:
    return {'error': str(e)}

if __name__ == '__main__':
    app.run(host="192.168.50.222", port=port)
