// Entity path tests
using Xunit;
using Unity.RerunSDK.Core;

public class EntityPathTests
{
    [Fact]
    public void Root_path_is_slash()
    {
        var p = new RerunEntityPath("/");
        Assert.Equal("/", p.Value);
    }

    [Fact]
    public void Simple_path_normalized()
    {
        var p = new RerunEntityPath("world/cube");
        Assert.Equal("world/cube", p.Value);
    }

    [Fact]
    public void Leading_and_trailing_slashes_trimmed()
    {
        var p = new RerunEntityPath("//world/cube//");
        Assert.Equal("world/cube", p.Value);
    }

    [Fact]
    public void Empty_path_is_root()
    {
        var p = new RerunEntityPath("");
        Assert.Equal("/", p.Value);
    }

    [Fact]
    public void Empty_segment_throws()
    {
        Assert.Throws<System.ArgumentException>(() => new RerunEntityPath("world//cube"));
    }

    [Fact]
    public void Double_underscore_prefix_rewritten()
    {
        var p = new RerunEntityPath("__reserved/path");
        Assert.StartsWith("_user", p.Value);
    }
}
