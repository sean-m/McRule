using System.Text;
using System.Text.RegularExpressions;

namespace SMM {
    public static class FilterPatternHelpers {

        private static bool HasFilterCharacters(string input) { return Regex.IsMatch(input, @"(^([~\*]+)|(\*)$)"); }
        public static string AddFilterOptionsIfNotSpecified(this string Pattern, FilterOptions options = FilterOptions.None) {
            string result = Pattern.Trim();

            if (HasFilterCharacters(Pattern)) return result;

            if (options.HasFlag(FilterOptions.Contains)) {
                result = $"*{result}*";
            } else {
                if (options.HasFlag(FilterOptions.StartsWith)) {
                    result = $"{result}*";
                }
                if (options.HasFlag(FilterOptions.EndsWith)) {
                    result = $"*{result}";
                }
            }
            if (options.HasFlag(FilterOptions.IgnoreCase)) {
                result = $"~{result}";
            }

            return result;
        }

        public enum FilterOptions {
            None = 0,
            IgnoreCase = 1,
            Contains = 2,
            StartsWith = 4,
            EndsWith = 8,
        }
    }

    public static class ConsoleHelpers {

        public static string ToKvText(this object entry) {
            if (entry == null) return string.Empty;

            StringBuilder sb = new StringBuilder();

            var properties = entry.GetType().GetProperties();
            var longestPropertyLength = properties.Select(x => x.Name.Length).Max();
            int indentation = longestPropertyLength + 3;

            foreach (var p in properties) {
                sb.Append(p.Name.PadRight(longestPropertyLength));
                sb.Append(" : ");
                bool multiLine = false;
                var thing = p.GetValue(entry);
                if (thing is string) {
                    foreach (var value in (thing ?? "NULL").ToString().Split("\n")) {
                        var strValue = FormatProperty((value) ?? "NULL");
                        if (multiLine) {
                            sb.AppendLine((strValue).PadLeft(indentation + strValue.Length));
                        } else {
                            sb.AppendLine((strValue).PadRight(longestPropertyLength));
                            multiLine = true;
                        }
                    }
                } else {
                    var value = FormatProperty(thing);
                    sb.AppendLine((value).PadLeft(value.Length));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Format properties for being printed inside a table. For array objects with fewer than 4
        /// elements, wrap em in square brackets and comma delimit them. 4 or more, take the first
        /// three that way then append ellipsis and the number of elements. This is more for
        /// debugging than anything else.
        /// </summary>
        /// <param name="Property"></param>
        /// <returns></returns>
        private static string FormatProperty(dynamic Property) {
            if (Property == null) return "*NULL";

            if (Property is String s) return s;

            if (Property.GetType().GetInterface("IEnumerable") != null) {
                if (Enumerable.Count(Property) <= 5) {
                    return $"[{String.Join(", ", Property)}]";
                } else {
                    var elems = new List<string>();
                    var count = 0;
                    foreach (var el in Property) {
                        count++;
                        elems.Add(el.ToString());
                        if (count >= 3) break;
                    }

                    return $"[{String.Join(", ", elems)}...({Enumerable.Count(Property)})]";
                }
            }

            return Property?.ToString();
        }
    }
}

