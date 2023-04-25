#!/bin/bash
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-7.15.1-linux-x86_64.tar.gz
tar xzvf elasticsearch-7.15.1-linux-x86_64.tar.gz
useradd elasticsearch
kill -s 9 `pgrep elasticsearch`
chown -R elasticsearch:elasticsearch elasticsearch-7.15.1
cd elasticsearch-7.15.1/
su elasticsearch -c "bin/elasticsearch -d"
sleep 30
curl http://127.0.0.1:9200
