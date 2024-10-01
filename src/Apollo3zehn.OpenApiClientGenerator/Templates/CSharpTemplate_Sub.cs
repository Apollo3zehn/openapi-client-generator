namespace {{{Namespace}}}
{

/// <summary>
/// A client for version {{{Version}}}.
/// </summary>
public interface I{{{Version}}}
{
{{{SubClientInterfaceProperties}}}
}

/// <inheritdoc />
public class {{{Version}}} : I{{{Version}}}
{
    /// <summary>
    /// Initializes a new instance of the <see cref="{{{Version}}}"/>.
    /// </summary>
    /// <param name="client">The client to use.</param>
    public {{{Version}}}({{{ClientName}}}Client client)
    {
{{{SubClientPropertyAssignments}}}
    }

{{{SubClientProperties}}}
}

{{{SubClientSource}}}

{{{Models}}}

}