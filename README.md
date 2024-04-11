# Developing with ElasticSearch

You can run on local machine, run with docker or use an instance on the cloud. Check documentation at [https://www.elastic.co/](https://www.elastic.co/)

## Elasticsearch Version used to develop this driver

At the date of today the latest version that you can download is used, no work has been done to check compatibility with previous versions.

## A ui to elastic

If you need an UI to manage your elastic search, you can use elasticsearch ui running in docker

```bash
docker run -d -p 5000:5000 elastichq/elasticsearch-hq
```

Then connect to the local instance of elastic at http://host.docker.internal:9200

You can also use kibana or other UI of your choice.