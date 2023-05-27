using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Semtex.Semantics;

public class SingleDocumentFixAllDiagnosticProvider: FixAllContext.DiagnosticProvider
{
    private readonly IEnumerable<Diagnostic> _diagnostics;
    private readonly DocumentId _id;

    public SingleDocumentFixAllDiagnosticProvider(IEnumerable<Diagnostic> diagnostics, DocumentId id)
    {
        _diagnostics = diagnostics;
        _id = id;
    }

    public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
    {
        if (document== null || document.Id != _id)
            throw new ArgumentException(
                $"{nameof(SingleDocumentFixAllDiagnosticProvider)} is only setup for document with id {_id} this document has Id {document.Id}");
        
        return Task.FromResult(_diagnostics);
    }

    public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
    {
        return Task.FromResult(_diagnostics);
    }
}