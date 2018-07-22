using System.Collections.Generic;

/**
 * A container for our parse results which allows us access to the
 * optional argument data and the positional argument data. It
 * also has a nested ParseResult member which is set when
 * there was a parsed sub-command with its own parse results.
 */
namespace cgParse {

public class ParseResult {

    public Dictionary<string, ArgData> Options{ get; protected set; }
    public Dictionary<string, ArgData> Positionals{ get; protected set; }
    public string SubPredicate{ get; protected set; }
    public ParseResult SubResult{ get; protected set; }

    /**
     * Use this ctor if there are no more nested parse results.
     * The SubResult property is then set to null.
     *
     * @param opts: the options to assign
     * @param positionals: the positionals to assign
     */
    public ParseResult(Dictionary<string, ArgData> opts, 
            Dictionary<string, ArgData> positionals) {
        Options = opts;
        Positionals = positionals;
        SubResult = null;
        SubPredicate = "";
    }

    /**
     * Use this ctor if there is a nested parse
     * result you want to add.
     *
     * @param opts: the options to assign
     * @param positionals: the positionals to assign
     * @param subPredicate: the predicate of the subcommand that was chosen
     * @param subResult: the nested parse result of the sub command
     */
    public ParseResult(Dictionary<string, ArgData> opts, 
            Dictionary<string, ArgData> positionals,
            string subPredicate,
            ParseResult subResult) {
        Options = opts;
        Positionals = positionals;
        SubPredicate = subPredicate;
        SubResult = subResult;
    }
}

}
