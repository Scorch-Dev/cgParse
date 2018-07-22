using System;
using System.Collections.Generic;

/**
 * An inheritable interface allows us to have command arguments of arbitrary backing-type. 
 * These objects are generally stored in dictionaries and are always 
 * referenceable by their LongName property when in these dictionaries. 
 * The end result is a system similar to ArgParse in Python.
 */
namespace cgParse {

public class ArgDescriptor {

    //followings args only used for array-type backing data
    public bool AppendMode{ get; protected set; }
    public int MinArgs{ get; protected set; }
    public int MaxArgs{ get; protected set; }
    public bool IsWellDefined{ get{ return ((MinArgs == MaxArgs) && (MinArgs > -1)); } }

    public ArgData Data{ get; protected set; }
    public string LongName{ get; protected set; }
    public string HelpText{ get; protected set; }

    /**
     * Ctors with simple (non-array) backing fields
     *
     * @param longName: the name by which the option is referenced
     * @param dataType: the datatype that this will take on. Backing data sets to default
     * @param appendMode: whether or not to append to 
     *  (only works if backing filed is array typed) (default: false)
     * @param minArgs: minimum number of args taken or -1 if there is no minimum 
     *  (only works if backing filed is array typed) (default: -1)
     * @param maxArgs: maximum number of args taken or -1 if there is no maximum 
     *  (only works if backing filed is array typed) (default: -1)
     * @param helpText: the help text to display on help (default: "")
     */
    public ArgDescriptor(
            string longName, ArgData.DataType dataType, 
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, string helpText = "")
    : this(longName, appendMode, helpText, minArgs, maxArgs) {

            Data = new ArgData(dataType);
    }

    /**
     * @param longName: the name by which the option is referenced
     * @param defaultVal: the default value to take. May be any int, int[], string, string[], float, float[], bool
     * @param helpText: the help text to display on help (default: "")
     */
    public ArgDescriptor(
            string longName, int defaultVal,
            string helpText = "")
    : this(longName, false, helpText, 0, 0) {

        Data = new ArgData(defaultVal);
    }
    public ArgDescriptor(
            string longName, string defaultVal,
            string helpText = "")
    : this(longName, false, helpText, 0, 0) {

        Data = new ArgData(defaultVal);
    }
    public ArgDescriptor(
            string longName, float defaultVal,
            string helpText = "")
    : this(longName, false, helpText, 0, 0) {

        Data = new ArgData(defaultVal);
    }
    public ArgDescriptor(
            string longName, bool defaultVal,
            string helpText = "")
    : this(longName, false, helpText, 0, 0) {

        Data = new ArgData(defaultVal);
    }


    /**
     * A ctor for array-backed data args
     *
     * @param longName: the name by which the option is referenced
     * @param defaultVal: the default value to take.
     * @param appendMode: whether or not to append or replace the default value on parse
     *  (default: false)
     * @param minArgs: the minimum arguments that the array can take (default: -1).
     *      A value of -1 means no limitation.
     * @param minArgs: the maximum arguments that the array can take (default: -1).
     *      A value of -1 means no limitation.
     * @param helpText: the help text to display on help
     */
    public ArgDescriptor(
            string longName, int[] defaultVal, 
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, string helpText = "")
    : this(longName, appendMode, helpText, minArgs, maxArgs) {
        Data = new ArgData(defaultVal);
    }
    public ArgDescriptor(
            string longName, float[] defaultVal,
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, string helpText = "")
    : this(longName, appendMode, helpText, minArgs, maxArgs) {

        Data = new ArgData(defaultVal);
    }
    public ArgDescriptor(
            string longName, string[] defaultVal,
            bool appendMode=false, int minArgs=-1, int maxArgs=-1, string helpText = "")
    : this(longName, appendMode, helpText, minArgs, maxArgs) {

        Data = new ArgData(defaultVal);
    }


    //helper to reduce verbosity in other ctors
    private ArgDescriptor(string longName, bool appendMode, 
            string helpText, int minArgs, int maxArgs){

        LongName = longName;
        HelpText = helpText;
        AppendMode = appendMode;
        MaxArgs = maxArgs;
        MinArgs = minArgs;
    }
}

}
