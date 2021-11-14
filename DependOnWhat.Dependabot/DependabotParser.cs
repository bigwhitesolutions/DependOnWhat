namespace DependOnWhat.Dependabot
{
    using System.IO.Enumeration;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public static class DependabotParser
    {
        public static Dependabot ParseYaml(string configText)
        {
            var deserializer = new DeserializerBuilder()
                           .WithNamingConvention(CamelCaseNamingConvention.Instance)
                           .Build();

            return deserializer.Deserialize<Dependabot>(configText);
        }

        public static IEnumerable<string> GetVersionsToIgnore(this Dependabot dependabot, string dependancyName)
        {
            var ignore = dependabot
                .Updates
                .First()
                .Ignores
                .FirstOrDefault(x => FileSystemName.MatchesSimpleExpression(x.DependencyName, dependancyName));

            return ignore == null
                ? Enumerable.Empty<string>()
                : ignore.Versions;
        }
    }
}