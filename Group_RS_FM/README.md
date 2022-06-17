# README Gruppe Flavio Müller und Ronny Schneeberger

## data_prep.ipynb
Das Notebook data_prep.ipynb enthält alle Vorbereitungsschritte der Daten. Folgende Transformationen werden angewendet:

* Erkennung der einzelnen, aufgenommenen Aktivitäten
* Aufteilen aller Aktivitäten in Samples mit dem Shape (43,13) was zu einer Länge von ca. 2.5 Sekunden führt
* Samples erneut aufteilen, diesmal mit einem Offset der halben Samplelänge
* StratifiedShuffleSplit der Daten 

Die Daten wurden im Anschluss auf das Azure Portal hochgeladen. Sie sind nicht im GitLab Repository vorhanden, da sie zu gross sind. Sie werden zu Beginn der beiden cnn Notebooks mit folgendem Snippet heruntergeladen:

```python
import os
from azure.storage.blob import ContainerClient, __version__

key = "DefaultEndpointsProtocol=https;AccountName=del2984826843;AccountKey=kSYohscGrcofRDfc45qylMvhfgaXZt2H37ofNaUsZHHWhN695wArQWA0YfgQvxN9IlFOPI3OnKE70w5xsC7GXw==;EndpointSuffix=core.windows.net"

if "test.npz" not in os.listdir() and "train.npz" not in os.listdir():
    try:
        print("Azure Blob Storage v" + __version__)
        # Quick start code goes here
        cc = ContainerClient.from_connection_string(conn_str=key, container_name='data-fm-rs')

        with open("train.npz", "wb") as f:
            f.write(cc.download_blob('train.npz').content_as_bytes())
        with open("test.npz", "wb") as f:
            f.write(cc.download_blob('test.npz').content_as_bytes())

    except Exception as ex:
        print('Exception:')
        print(ex)
```

## cnn.ipynb

In diesem Notebook werden CNNs mit verschiedenen Parameter trainiert und Resultate interpretiert. 

## cnn_savgol.ipynb

Ähnlicher Aufbau wie das Notebook cnn.ipynb. Die Performance auf den gefilterten Daten lieferte jedoch keine signifikante Verbesserung der Performance, weshalb diese Strategie nicht weiter verfolgt wurde.
