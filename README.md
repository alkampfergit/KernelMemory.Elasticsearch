# Use ElasticSeach with Kernel Memory

In this project there is an opinionated implementation of IMemoryDb for [Microsoft Kernel Memory](https://github.com/microsoft/kernel-memory) based on the latest version of ElasticSearch >=8.13. This implementation was tested only on the latest version of ES 8, and it is not meant to work with older version.

This library implements only the Memory DB part, so you still need to configure a different database for Binary Storage in Kernel Memory.

## Details of implementation

This implementation uses dynamic template mapping so we do not need to do nested object mapping, this was done **to have a better schema of the object and a real simple way to query the data**. 

## Search in payload

Kernel memory uses only tags to filter results, and the MemoryRecord **has payload dictionary that contains non searchable fields**. Actually this implementation was created to allow mixing vector search and BM25 search, so you have a special property called **IndexablePayloadProperties** in the configuration that allows you to specify which payload you want to be indexed and searchable.

## Developing with ElasticSearch

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