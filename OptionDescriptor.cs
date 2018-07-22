using System;
using System.Collections.Generic;

/**
 * Identical to ArgDescriptor except is has the ability
 * to be assigned an extra "ShortName" that can be used
 * instead of the long name. (e.g. "verbose" is a long name,
 * but 'v' is the short name. When passed into the command line
 * that turns into --verbose and -v).
 */
namespace cgParse {

public class OptionDescriptor : ArgDescriptor {

    public char ShortName{ get; protected set; }

    /**
     * A slight revamp of the ctors of the base class, allowing
     * us to assign a few extra options specific to optional arguments.
     *
     * @param longName: the long name to take
     * @param dataType: the underlying data type. Backing data takes default value
     * @param appendMode: whether to append or replace the underlying data. Only works
     *  with array backing data. (default : false)
     * @param minArgs: the minimum number of args to take if there is an array backing field.
     *  -1 if there is no minimum. (default: -1)
     * @param maxArgs: the maximum number of args to take if there is an array backing field.
     *  -1 if there is no maximum. (default: -1)
     * @param helpText: the help text to display for this item (default: "")
     * @param shortName: the short name to refer to this by (default: ' ')
     *  The short name must a letter or it is ignored.
     */
    public OptionDescriptor(
            string longName, ArgData.DataType dataType, 
            bool appendMode=false, int minArgs=-1, int maxArgs=-1,
            string helpText="", char shortName = ' ')
        : base(longName, dataType, appendMode, minArgs, maxArgs, helpText) {
            ShortName = shortName;
    }
    /**
     * @param longName: the long name to take
     * @param defaultVal: the default value this takes.
     * @param helpText: the help text to display for this item (default: "")
     * @param shortName: the short name to refer to this by (default: ' ')
     *  The short name must a letter or it is ignored.
     */
    public OptionDescriptor(string longName, int defaultVal, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, helpText) {
            ShortName = shortName;
    }
    public OptionDescriptor(string longName, string defaultVal, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, helpText) {
            ShortName = shortName;
    }
    public OptionDescriptor(string longName, float defaultVal, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, helpText) {
            ShortName = shortName;
    }
    public OptionDescriptor(string longName, bool defaultVal, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, helpText) {
            ShortName = shortName;
    }


    /**
     * Array-type-backing ctors
     *
     * @param longName: the long name to take
     * @param defaultVal: default value to take, which determines the type (int[], float[], or string[])
     * @param appendMode: whether to append or replace the underlying data. Only works
     *  with array backing data. (default : false)
     * @param minArgs: the minimum number of args to take if there is an array backing field.
     *  -1 if there is no minimum. (default: -1)
     * @param maxArgs: the maximum number of args to take if there is an array backing field.
     *  -1 if there is no maximum. (default: -1)
     * @param helpText: the help text to display for this item (default: "")
     * @param shortName: the short name to refer to this by (default: ' ')
     *  The short name must a letter or it is ignored.
     */
    public OptionDescriptor(string longName, int[] defaultVal, 
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, appendMode, minArgs, maxArgs, helpText) {
            ShortName = shortName;
    }
    public OptionDescriptor(string longName, string[] defaultVal,
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, appendMode, minArgs, maxArgs, helpText) {
            ShortName = shortName;
    }
    public OptionDescriptor(string longName, float[] defaultVal,
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, 
            string helpText = "", char shortName = ' ')
    : base(longName, defaultVal, appendMode, minArgs, maxArgs, helpText) {
            ShortName = shortName;
    }
}

}
