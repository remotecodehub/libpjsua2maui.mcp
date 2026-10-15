namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Identifies the producer and pjproject revisions used to enrich package source evidence.</summary>
public sealed record SourceRepositoryDescriptor
{
    public SourceRepositoryDescriptor(
        string rootPath,
        string repository,
        string revision,
        string tag,
        string pjprojectRootPath,
        string pjprojectRepository,
        string pjprojectRevision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(pjprojectRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pjprojectRepository);
        ArgumentException.ThrowIfNullOrWhiteSpace(pjprojectRevision);

        RootPath = Path.GetFullPath(rootPath);
        Repository = repository;
        Revision = revision;
        Tag = tag;
        PjprojectRootPath = Path.GetFullPath(pjprojectRootPath);
        PjprojectRepository = pjprojectRepository;
        PjprojectRevision = pjprojectRevision;
    }

    public string RootPath { get; }
    public string Repository { get; }
    public string Revision { get; }
    public string Tag { get; }
    public string PjprojectRootPath { get; }
    public string PjprojectRepository { get; }
    public string PjprojectRevision { get; }
}
