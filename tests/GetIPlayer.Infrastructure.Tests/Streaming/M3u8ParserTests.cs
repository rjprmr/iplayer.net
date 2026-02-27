using FluentAssertions;
using GetIPlayer.Infrastructure.Streaming;

namespace GetIPlayer.Infrastructure.Tests.Streaming;

public class M3u8ParserTests
{
    private static readonly Uri BaseUri = new("https://example.com/stream/");

    [Fact]
    public void ParseMasterPlaylist_WithValidContent_ReturnsVariants()
    {
        var content = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=1280000,RESOLUTION=640x360,CODECS="avc1.4d401e,mp4a.40.2"
            low/index.m3u8
            #EXT-X-STREAM-INF:BANDWIDTH=2560000,RESOLUTION=1280x720,CODECS="avc1.4d401f,mp4a.40.2"
            mid/index.m3u8
            #EXT-X-STREAM-INF:BANDWIDTH=5120000,RESOLUTION=1920x1080,CODECS="avc1.640029,mp4a.40.2"
            high/index.m3u8
            """;

        var result = M3u8Parser.ParseMasterPlaylist(content, BaseUri);

        result.Should().HaveCount(3);
        result[0].Bandwidth.Should().Be(1280000);
        result[0].Width.Should().Be(640);
        result[0].Height.Should().Be(360);
        result[1].Bandwidth.Should().Be(2560000);
        result[2].Bandwidth.Should().Be(5120000);
    }

    [Fact]
    public void ParseMasterPlaylist_ResolvesRelativeUrls()
    {
        var content = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=1280000
            low/index.m3u8
            """;

        var result = M3u8Parser.ParseMasterPlaylist(content, BaseUri);

        result.Should().HaveCount(1);
        result[0].Url.ToString().Should().Be("https://example.com/stream/low/index.m3u8");
    }

    [Fact]
    public void ParseMasterPlaylist_ResolvesAbsoluteUrls()
    {
        var content = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=1280000
            https://cdn.example.com/stream.m3u8
            """;

        var result = M3u8Parser.ParseMasterPlaylist(content, BaseUri);

        result.Should().HaveCount(1);
        result[0].Url.ToString().Should().Be("https://cdn.example.com/stream.m3u8");
    }

    [Fact]
    public void ParseMasterPlaylist_WithEmptyContent_ThrowsArgumentException()
    {
        var act = () => M3u8Parser.ParseMasterPlaylist("", BaseUri);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseMasterPlaylist_WithNullBaseUri_ThrowsArgumentNullException()
    {
        var act = () => M3u8Parser.ParseMasterPlaylist("#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=1\ntest.m3u8", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseMediaPlaylist_WithValidContent_ReturnsSegments()
    {
        var content = """
            #EXTM3U
            #EXT-X-TARGETDURATION:10
            #EXT-X-MEDIA-SEQUENCE:0
            #EXTINF:9.009,
            segment0.ts
            #EXTINF:9.009,
            segment1.ts
            #EXTINF:3.003,
            segment2.ts
            #EXT-X-ENDLIST
            """;

        var result = M3u8Parser.ParseMediaPlaylist(content, BaseUri);

        result.Should().HaveCount(3);
        result[0].Duration.Should().BeApproximately(9.009, 0.001);
        result[0].SequenceNumber.Should().Be(0);
        result[1].SequenceNumber.Should().Be(1);
        result[2].Duration.Should().BeApproximately(3.003, 0.001);
    }

    [Fact]
    public void ParseMediaPlaylist_RespectsMediaSequence()
    {
        var content = """
            #EXTM3U
            #EXT-X-MEDIA-SEQUENCE:100
            #EXTINF:10.0,
            segment.ts
            """;

        var result = M3u8Parser.ParseMediaPlaylist(content, BaseUri);

        result.Should().HaveCount(1);
        result[0].SequenceNumber.Should().Be(100);
    }

    [Fact]
    public void ParseMediaPlaylist_WithEmptyContent_ThrowsArgumentException()
    {
        var act = () => M3u8Parser.ParseMediaPlaylist("", BaseUri);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsMasterPlaylist_WithMasterContent_ReturnsTrue()
    {
        var content = "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=1000\ntest.m3u8";
        M3u8Parser.IsMasterPlaylist(content).Should().BeTrue();
    }

    [Fact]
    public void IsMasterPlaylist_WithMediaContent_ReturnsFalse()
    {
        var content = "#EXTM3U\n#EXTINF:10.0,\nsegment.ts";
        M3u8Parser.IsMasterPlaylist(content).Should().BeFalse();
    }

    [Fact]
    public void ParseMasterPlaylist_WithCodecs_ParsesCodecs()
    {
        var content = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=1280000,CODECS="avc1.4d401e,mp4a.40.2"
            test.m3u8
            """;

        var result = M3u8Parser.ParseMasterPlaylist(content, BaseUri);

        result[0].Codecs.Should().Be("avc1.4d401e,mp4a.40.2");
    }

    [Fact]
    public void ParseMasterPlaylist_WithoutResolution_ReturnsNullDimensions()
    {
        var content = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=128000
            audio.m3u8
            """;

        var result = M3u8Parser.ParseMasterPlaylist(content, BaseUri);

        result[0].Width.Should().BeNull();
        result[0].Height.Should().BeNull();
    }
}
