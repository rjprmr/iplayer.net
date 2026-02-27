using FluentAssertions;
using GetIPlayer.Infrastructure.Streaming;

namespace GetIPlayer.Infrastructure.Tests.Streaming;

public class MpdParserTests
{
    private static readonly Uri BaseUri = new("https://example.com/dash/");

    [Fact]
    public void ParseMpd_WithValidMpd_ReturnsAdaptationSets()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011" mediaPresentationDuration="PT1H30M">
              <Period>
                <AdaptationSet mimeType="video/mp4" lang="en">
                  <Representation id="1" bandwidth="5000000" width="1920" height="1080" codecs="avc1.640029">
                    <BaseURL>video_high.mp4</BaseURL>
                  </Representation>
                  <Representation id="2" bandwidth="2500000" width="1280" height="720" codecs="avc1.4d401f">
                    <BaseURL>video_mid.mp4</BaseURL>
                  </Representation>
                </AdaptationSet>
                <AdaptationSet mimeType="audio/mp4">
                  <Representation id="3" bandwidth="128000" codecs="mp4a.40.2">
                    <BaseURL>audio.mp4</BaseURL>
                  </Representation>
                </AdaptationSet>
              </Period>
            </MPD>
            """;

        var result = MpdParser.ParseMpd(xml, BaseUri);

        result.Should().HaveCount(2);
        result[0].MimeType.Should().Be("video/mp4");
        result[0].Representations.Should().HaveCount(2);
        result[0].Representations[0].Bandwidth.Should().Be(5000000);
        result[0].Representations[0].Width.Should().Be(1920);
        result[0].Representations[0].Height.Should().Be(1080);
        result[1].MimeType.Should().Be("audio/mp4");
        result[1].Representations.Should().HaveCount(1);
    }

    [Fact]
    public void ParseMpd_WithBaseURL_ResolvesRelativeUrls()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011">
              <Period>
                <AdaptationSet mimeType="video/mp4">
                  <Representation id="1" bandwidth="1000000">
                    <BaseURL>video.mp4</BaseURL>
                  </Representation>
                </AdaptationSet>
              </Period>
            </MPD>
            """;

        var result = MpdParser.ParseMpd(xml, BaseUri);

        result[0].Representations[0].BaseUrl.Should().NotBeNull();
        result[0].Representations[0].BaseUrl!.ToString().Should().Be("https://example.com/dash/video.mp4");
    }

    [Fact]
    public void ParseMpd_WithInvalidXml_ReturnsEmpty()
    {
        var result = MpdParser.ParseMpd("not xml at all", BaseUri);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseMpd_WithEmptyContent_ThrowsArgumentException()
    {
        var act = () => MpdParser.ParseMpd("", BaseUri);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseMpd_WithNullBaseUri_ThrowsArgumentNullException()
    {
        var act = () => MpdParser.ParseMpd("<MPD/>", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseDuration_WithValidDuration_ReturnsTimeSpan()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011" mediaPresentationDuration="PT1H30M15S" />
            """;

        var result = MpdParser.ParseDuration(xml);

        result.Should().NotBeNull();
        result!.Value.Should().Be(new TimeSpan(1, 30, 15));
    }

    [Fact]
    public void ParseDuration_WithNoDuration_ReturnsNull()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011" />
            """;

        var result = MpdParser.ParseDuration(xml);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseDuration_WithNullOrEmpty_ReturnsNull()
    {
        MpdParser.ParseDuration(null!).Should().BeNull();
        MpdParser.ParseDuration("").Should().BeNull();
    }

    [Fact]
    public void ParseDuration_WithInvalidXml_ReturnsNull()
    {
        MpdParser.ParseDuration("not xml").Should().BeNull();
    }

    [Fact]
    public void ParseMpd_WithSegmentTemplate_ParsesSegmentUrls()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011">
              <Period>
                <AdaptationSet mimeType="video/mp4">
                  <Representation id="video1" bandwidth="5000000">
                    <SegmentTemplate media="seg_$Time$.m4s" initialization="init_$RepresentationID$.m4s">
                      <SegmentTimeline>
                        <S t="0" d="96000" r="2" />
                      </SegmentTimeline>
                    </SegmentTemplate>
                  </Representation>
                </AdaptationSet>
              </Period>
            </MPD>
            """;

        var result = MpdParser.ParseMpd(xml, BaseUri);

        result.Should().HaveCount(1);
        result[0].Representations[0].SegmentUrls.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void ParseMpd_WithLanguageAttribute_ParsesLang()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011">
              <Period>
                <AdaptationSet mimeType="audio/mp4" lang="en">
                  <Representation id="1" bandwidth="128000" />
                </AdaptationSet>
              </Period>
            </MPD>
            """;

        var result = MpdParser.ParseMpd(xml, BaseUri);

        result[0].Lang.Should().Be("en");
    }
}
