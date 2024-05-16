using DocumentFormat.OpenXml.Drawing.Diagrams;
using KernelMemory.ElasticSearch.FunctionalTests.Doubles;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;

namespace KernelMemory.ElasticSearch.FunctionalTests;

[Trait("Category", "Bulk")]
public class ElasticSearchBulkMemoryApiTests : BasicElasticTestFixture
{
    private readonly TestEmbeddingGenerator _embeddingGenerator;
    private readonly ElasticSearchMemory _sut;
    private readonly string _indexName;

    public ElasticSearchBulkMemoryApiTests(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
    {
        _embeddingGenerator = new TestEmbeddingGenerator();
        _sut = new ElasticSearchMemory(Config, _embeddingGenerator);

        _indexName = nameof(ElasticSearchBulkMemoryApiTests).ToLower();

        ////going to delete the index if exists
        //_sut.DeleteIndexAsync(_indexName, CancellationToken.None).Wait();

        _sut.CreateIndexAsync(_indexName, 3, CancellationToken.None).Wait();
    }

    [Fact]
    public async Task Can_bulk_insert_different_documents()
    {
        MemoryRecord mr1 = GenerateAMemoryRecord(documentId: "AA");
        MemoryRecord mr2 = GenerateAMemoryRecord(documentId: "BB");

        await _sut.UpsertManyAsync(_indexName, new[] { mr1, mr2 }, CancellationToken.None);

        //now reload both record 
        MemoryFilter docFilter = new();
        docFilter.ByDocument("AA");
        var reloaded = _sut.GetListAsync(_indexName, [docFilter], limit: 10, cancellationToken: CancellationToken.None, withEmbeddings: true);
        var list = await reloaded.ToListAsync();
        CompareMemoryRecords(mr1, list.Single());

        //now reload secondo memory record
        docFilter = new();
        docFilter.ByDocument("BB");
        reloaded = _sut.GetListAsync(_indexName, [docFilter], limit: 10, cancellationToken: CancellationToken.None, withEmbeddings: true);
        list = await reloaded.ToListAsync();
        CompareMemoryRecords(mr2, list.Single());
    }

    [Fact]
    public async Task Replace_documents_without_previous_documents()
    {
        MemoryRecord mr1 = GenerateAMemoryRecord(documentId: "DD");
        MemoryRecord mr2 = GenerateAMemoryRecord(documentId: "DD");

        await _sut.UpsertManyAsync(_indexName, new[] { mr1, mr2 }, CancellationToken.None);

        await _sut.ReplaceDocumentAsync(_indexName, "DD", [mr1, mr2]);

        var realIndexNames = $"{Config!.IndexPrefix}{_indexName}";
        await this.ElasticSearchHelper.RefreshAsync(realIndexNames);

        //now reload both record
        MemoryFilter docFilter = new();
        docFilter.ByDocument("DD");
        var reloaded = _sut.GetListAsync(_indexName, [docFilter], limit: 10, cancellationToken: CancellationToken.None, withEmbeddings: true);
        var list = await reloaded.ToListAsync();

        Assert.Equal(2, list.Count);

        var m1reload = list.Single(m => m.Id == mr1.Id);
        CompareMemoryRecords(mr1, m1reload);

        var m3Reloaded = list.Single(m => m.Id == mr2.Id);
        CompareMemoryRecords(mr2, m3Reloaded);
    }

    [Fact]
    public async Task Can_replace_documents()
    {
        MemoryRecord mr1 = GenerateAMemoryRecord(documentId: "CC");
        MemoryRecord mr2 = GenerateAMemoryRecord(documentId: "CC");
        MemoryRecord mr3 = GenerateAMemoryRecord(documentId: "CC");

        var realIndexNames = $"{Config!.IndexPrefix}{_indexName}";

        await _sut.UpsertManyAsync(_indexName, new[] { mr1, mr2 }, CancellationToken.None);
        await this.ElasticSearchHelper.RefreshAsync(realIndexNames);

        await _sut.ReplaceDocumentAsync(_indexName, "CC", [mr1, mr3]);
        await this.ElasticSearchHelper.RefreshAsync(realIndexNames);

        //now reload both record
        MemoryFilter docFilter = new();
        docFilter.ByDocument("CC");
        var reloaded = _sut.GetListAsync(_indexName, [docFilter], limit: 10, cancellationToken: CancellationToken.None, withEmbeddings: true);
        var list = await reloaded.ToListAsync();

        Assert.Equal(2, list.Count);

        var m1reload = list.Single(m => m.Id == mr1.Id);
        CompareMemoryRecords(mr1, m1reload);

        var m3Reloaded = list.Single(m => m.Id == mr3.Id);
        CompareMemoryRecords(mr3, m3Reloaded);

        //now we should not find mr2
        Assert.DoesNotContain(list, m => m.Id == mr2.Id);
    }
}
