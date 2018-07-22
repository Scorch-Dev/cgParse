using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * The cli command is a way to encapsulate a single command.
 * It can be assigned optional arguments, positional arguments,
 * etc. It takes care of parsing arguments for itself, and it
 * can also have sub-commands attached to it which do their
 * own parsing.
 *
 * When this is all said and done, the main output to be concerned
 * with is the ParseResult, which contains the data coming from our
 * nested commands.
 */
namespace cgParse {

public class Command {
    
    /************************************************************
     * OPTIONS AND POSITIONAL ARGUMENTS
     ************************************************************/

    //predicate usually used to reference this command when it's a sub command
    public string Predicate{ get; protected set;}

    private Dictionary<string, ArgDescriptor> OptDescriptors{ get; set; } //option descriptors
    private Dictionary<string, ArgDescriptor> PosDescriptors{ get; set; } //positional descriptors
    private LinkedList<ArgDescriptor> PosDescriptorsList{ get; set; } //duplicate data helps maintain ordering

    public IEnumerable<ArgDescriptor> PositionalArgs{ get { return PosDescriptorsList; } }
    public IEnumerable<ArgDescriptor> OptionalArgs{ get { return OptDescriptors.Values.Distinct(); } }

    private HelpTextFormatter _formatter;
    public HelpTextFormatter Formatter{ 
        get {
            return _formatter;
        }
        set { //propogate formatter change 
            _formatter = value;
            foreach(Command cmd in SubCommands.Values)
                cmd.Formatter = _formatter;
        }
    }

    /**
     * ctor
     *
     * @param predicate: the predicate that this command will take (default: "")
     */
    public Command(string predicate="", HelpTextFormatter formatter = null) {
        Predicate = predicate;
        PosDescriptors = new Dictionary<string, ArgDescriptor>();
        PosDescriptorsList = new LinkedList<ArgDescriptor>();
        OptDescriptors = new Dictionary<string, ArgDescriptor>();

        SubCommands = new Dictionary<string, Command>();

        AddOption(new OptionDescriptor("help", false, shortName:'h', helpText:"shows this help text and exits."));
        Formatter = formatter;
    }

    /**
     * These two copy functions are used to perform a deep copy
     * of hte underlying data. This way, commands are re-usable
     * between invocations of the parse, because the parse results
     * only contains copies of the data and its default values, which is
     * what the underlying dictionaries store.
     *
     * @returns a Dictionary containing the copied underlying data.
     */
    public Dictionary<string, ArgData> CreateOptionData(){
        return CreateArgData(true);
    }

    public Dictionary<string, ArgData> CreatePositionalData(){
        return CreateArgData(false);
    }

    /**
     * implementation of the above two
     *
     * @param getOptions: true to get options, false to get positionals
     */    
    private Dictionary<string, ArgData> CreateArgData(bool getOptions) {

        Dictionary<ArgData, string> alreadyCopiedRefs =
            new Dictionary<ArgData, string>();
        Dictionary<string, ArgData> copiedDict =
            new Dictionary<string, ArgData>();

        Dictionary<string, ArgDescriptor> sourceDict = (getOptions == true) ? OptDescriptors : PosDescriptors;

        foreach(KeyValuePair<string, ArgDescriptor> kvPair in sourceDict) {
            if(alreadyCopiedRefs.ContainsKey(kvPair.Value.Data)) {
                string refKey = alreadyCopiedRefs[kvPair.Value.Data];
                copiedDict.Add(kvPair.Key, copiedDict[refKey]);
            }
            else {
                copiedDict.Add(kvPair.Key, kvPair.Value.Data);
                alreadyCopiedRefs.Add(kvPair.Value.Data, kvPair.Key);
            }
        }
        return copiedDict;
    }

    /**
     * Adds multiple options without the option to
     * add reference names
     * @param optDescriptors: a params array of OptionDescriptor
     */
    public void AddOption(params OptionDescriptor[] optDescriptors) {
        foreach(OptionDescriptor optDesc in optDescriptors)
            AddOption(optDesc);
    }

    /**
     * Adds an optional to the options set.
     * @param optDesc: the description of the option to add
     * @param refName: an extra name to reference the option by after parsing.
     *  Is ignored if the reference name is "". (default: "")
     */
    public void AddOption(OptionDescriptor optDesc, string refName="") {

        //check if well-formed
        if(!ValidOption(optDesc))
            return;

        //add references as refName, long name, and short name
        if(refName != "" && ValidRefName(refName, true))
            OptDescriptors.Add(refName, optDesc);

        OptDescriptors.Add(optDesc.LongName, optDesc);
        if(optDesc.ShortName != ' ')
            OptDescriptors.Add(optDesc.ShortName.ToString(), optDesc);
    }

    /**
     * Adds multiple positionals without the option to
     * add reference names
     * @param argDescriptors: a params array of ArgDescriptor
     */
    public void AddPositional(params ArgDescriptor[] argDescriptors){ 
        foreach(ArgDescriptor argDesc in argDescriptors)
            AddPositional(argDesc);
    }

    /**
     * Adds an optional to the options set.
     * @param argDesc: the arg descriptor to use to add a positional
     * @param refName: an extra name to reference the argument by after parsing.
     *  Is ignored if the reference name is "". (default: "")
     */
    public void AddPositional(ArgDescriptor argDesc, string refName = "") {
        if(!ValidPositional(argDesc))
            return;
        
        //add references as refName, long name, and short name
        if(refName != "" && ValidRefName(refName, false))
            PosDescriptors.Add(refName, argDesc);

        PosDescriptors.Add(argDesc.LongName, argDesc);
        PosDescriptorsList.AddLast(argDesc);
    }

    /***************************************************************
     * ARG CHECKING
     ***************************************************************/

    bool ValidRefName(string refName, bool isOptional) {

        Dictionary<string, ArgDescriptor> argDescDict = (isOptional) ? OptDescriptors : PosDescriptors;
        if(argDescDict.ContainsKey(refName)) {
            WriteWarning(string.Format("Unable to add reference name '{0}'"
                    + " again because reference name already used"
                    + " by {1}", 
                    refName, argDescDict[refName].LongName));
            return false;
        }
        return true;
    }

    /**
     * Checks if a positional argument is valid to add. 
     * 
     * @param posDesc: the descriptor to check
     * @returns true if valid, false otherwise
     */
    private bool ValidPositional(ArgDescriptor posDesc) {

        if(!ValidArg(posDesc, false))
            return false;

        //list mpositional must be added last
        bool containsListPositional = false;
        LinkedListNode<ArgDescriptor> lastPosNode = PosDescriptorsList.Last;
        if(lastPosNode != null 
            && lastPosNode.Value.Data.SysType.IsArray
            && !lastPosNode.Value.IsWellDefined) {

            containsListPositional = true;
        }

        if(containsListPositional) {
            WriteWarning(string.Format(
                "Invalid positional arg {0}. There may only be one positional"
                + " list argument without a well-defined length and it must be added as the last"
                + " positional argument.", posDesc.LongName));
            return false;
        }

        return true;
    }
    
    /**
     * Checks if an optional argument can be added.
     *
     * @param optDesc: the descriptor to check
     * @returns true if valid to add, false otherwise
     */
    private bool ValidOption(OptionDescriptor optDesc) {
        
        if(!ValidArg(optDesc, true))
            return false;

        //short names must be unique letter for optional opt
        if(optDesc.ShortName != ' ') {
            if(!char.IsLetter(optDesc.ShortName)) {
                WriteWarning(string.Format("Invalid optional"
                    + " with long name '{0}'"
                    + " has non-letter short name '{1}'", optDesc.LongName, optDesc.ShortName));
                return false;
            }
            else if(OptDescriptors.ContainsKey(optDesc.ShortName.ToString())) {
                WriteWarning(string.Format("Invalid optional"
                    + " with long name '{0}' has non-unique short"
                    + " name '{1}' already in use by option with long name '{2}'", 
                    optDesc.LongName, optDesc.ShortName, 
                    OptDescriptors[optDesc.ShortName.ToString()].LongName));
                return false;
            }
        }

        return true;
    }

    /**
     * A helper to do some format checking common to all cli arguments.
     *
     * @param argDesc: the descriptor of the argument to check
     * @returns true if valid, false otherwise.
     */
    private bool ValidArg(ArgDescriptor argDesc, bool isOption) {

        Dictionary<string, ArgDescriptor> argDescDict = (isOption)  ? OptDescriptors : PosDescriptors;
        
        //long names must be unique
        if(argDescDict.ContainsKey(argDesc.LongName)) {
            WriteWarning(string.Format(
                "Invalid option has duplicate long name '{0}'",
                argDesc.LongName));
            return false;
        }
        
        //only chars allowed are [a-z][0-9], '-' and '_' (first letter cant be -)
        for(int i = 0; i < argDesc.LongName.Length; i++){

            char c = argDesc.LongName[i];

            if(i == 0 && c == '-') {
                WriteWarning(string.Format(
                    "Invalid arg with long name '{0}'"
                    + " cannot have long name starting with '-'", 
                    argDesc.LongName));
                return false;
            }

            if(!Char.IsLetter(c) && !Char.IsNumber(c)
                    && c != '-' && c != '_') {
                WriteWarning(string.Format(
                    "Invalid arg has long name '{0}' which contains a"
                    + " character other than [a-z][0-9] '-' and '_'.", 
                    argDesc.LongName));
                return false;
            }
        }

        return true;
    }

    /***********************************************************
     * SUB COMMANDS
     **********************************************************/

    public Dictionary<string, Command> SubCommands{ get; protected set; }

    /**
     * A safe interface for adding subcommands with safety checks.
     *
     * @param command: the Command object to add.
     */
    public void AddSubCommand(Command command) {
        //ensure command can be invoked
        if(command.Predicate == "") {
            string msg = string.Format(
                    "Invalid command, predicate can'be empty: {0}", 
                    command.GetType().ToString());
            WriteWarning(msg);
            return;
        }
        //no duplicates
        if(SubCommands.ContainsKey(command.Predicate)) {
            string msg = string.Format(
                    @"Failed to add command {0} because predicate
                    '{1}' is already assigned to command {2}",
                    command.GetType().ToString(),
                    command.Predicate,
                    SubCommands[command.Predicate].GetType().ToString());
            WriteWarning(msg);
            return;
        }

        //add if safe
        SubCommands.Add(command.Predicate, command);
        command.onWrite += (string s)=>{ Write(s); };
        command.Formatter = this.Formatter;
    }

    /**
     * Removes a sub command. Does nothing if
     * the sub command was never added.
     *
     * @param command: the command to remove
     */
    public void RemoveSubCommand(Command command) {
        RemoveSubCommand(command.Predicate);
    }
    /**
     * @param predicate: the predicate of the command to remove.
     */
    public void RemoveSubCommand(string predicate) {
        if(!SubCommands.ContainsKey(predicate))
            return;
        SubCommands.Remove(predicate);
    }

    /******************************************************************
     * PARSING ARGS
     ******************************************************************/

    /**
     * A struct that'll contains some useful information throughout the parse
     */
    private struct ParseData {
        public Dictionary<string, ArgData> optionsOut;
        public Dictionary<string, ArgData> positionalsOut;

        public int doubleDashIdx;
        public int maxArgIdx;
        public int subCommandIdx;

        public List<int> optIndexes;
        public LinkedList<int> argIndexes;
    }

    /**
     * The main parse method. Parsees tokenized arguments.
     *
     * @param args: the tokenized arguments as string[]
     * @param result: the output ParseResult object (null if failure)
     * @returns true on success, false otherwise.
     */
    public bool ParseArgs(string[] args, out ParseResult result) {

        result = null;
        bool ok;
        ParseData pd = new ParseData(){
            optionsOut = CreateOptionData(),
            positionalsOut = CreatePositionalData(),
            doubleDashIdx = -1,
            maxArgIdx = -1,
            subCommandIdx = -1,
            optIndexes = new List<int>(),
            argIndexes = new LinkedList<int>()
        };
        
        ok = CategorizeArgs(args, ref pd);
        if(!ok) //note no usage for this, since only failing condition is print --help
            return false;


        ok = ParseOptions(args, ref pd);
        if(!ok) {
            Write(HelpTextFormatter.Usage(this));
            return false;
        }

        ok = ParsePositionals(args, ref pd);
        if(!ok) {
            Write(HelpTextFormatter.Usage(this));
            return false;
        }

        ok = ParseSubCommand(args, ref pd, out result);
        if(!ok) //no usage for this, since fail condition has subparser write help
            return false;
        
        return ok;
    }

    /**
     * cateogorizes args into groups of options and args, placing them
     * in the passed in ParseData struct.
     * 
     * @param args: the tokenized arguments
     * @param pd: the ref ParseData for this parse run
     * @retursn true on success, false otherwise
     */
    private bool CategorizeArgs(string[] args, ref ParseData pd) {

        //skip predicate
        for(int i = 1; i < args.Length; i++) {

            if(pd.doubleDashIdx != -1) {
                pd.argIndexes.AddLast(i);
            }
            else if(args[i].StartsWith("-")) {
                if(pd.doubleDashIdx == -1 && args[i] == "--")
                    pd.doubleDashIdx = i;
                else if(args[i] == "--help" || args[i] == "-h") {
                    WriteHelp();
                    return false;
                }
                else
                    pd.optIndexes.Add(i);
            }
            else if(SubCommands.ContainsKey(args[i])) {
                pd.subCommandIdx = i; 
                break;//let the subcommand do further parse later
            }
            else
                pd.argIndexes.AddLast(i);
        }
        pd.maxArgIdx = (pd.subCommandIdx == -1) ? args.Length : pd.subCommandIdx;
        return true;
    }

    /**
     * parse all options and set the output data after args have
     * been categorized.
     *
     * @param args: the tokenized argument string
     * @param pd: the ref parse data for this parse run
     * @returns true on success, false otherwise.
     */
    private bool ParseOptions(string[] args, ref ParseData pd) {

        foreach(int idx in pd.optIndexes) {
            
            //get the opt and figure if it's long or short
            bool isLongOpt = args[idx].StartsWith("--"); 
            int nDashes = (isLongOpt) ? 2 : 1;
            string optString = args[idx].Substring(nDashes);

            bool ok = true;
            //list of bool parsedOptDescriptors
            if(!isLongOpt && optString.Length > 2) {
                ok = ParseShortOptionChain(optString, ref pd);
            }
            //bad arg
            else if(!pd.optionsOut.ContainsKey(optString))
                return false;
            //simple arg
            else if(!pd.optionsOut[optString].SysType.IsArray){
                ok = ParseSingleArg(OptDescriptors[optString], args, optString, idx+1, ref pd);
            }
            //array arg
            else {
                ArgDescriptor optDesc = OptDescriptors[optString];
                if(optDesc.IsWellDefined)
                    ok = ParseWellDefinedArrayArg(optDesc, args, optString, idx+1, ref pd);
                else {
                    ok = ParseListArg(optDesc, args, optString, idx+1, ref pd);
                }
            }

            if(!ok)
                return false;
        }

        return true;
    }

    /**
     * parse string of short names as bool
     *
     * @param optString: the tokenized argument string without the leading "-"
     * @param pd: the ref ParseData for this run
     * @returns true on success, false otherwise
     */
    private bool ParseShortOptionChain(string optString, ref ParseData pd) {

        for(int j = 0; j < optString.Length; j++) {
            string optShortName = optString[j].ToString();
            if(!pd.optionsOut.ContainsKey(optShortName))
                return false;
            else if(pd.optionsOut[optShortName].SysType != typeof(bool)) {
                Write("Only bool types with short names may be chained like '-abcd'.");
                Write(string.Format("Please use -{0} <argument> instead.", optShortName));
                return false;
            }
            else //set bool val if flagged
                pd.optionsOut[optShortName].SetValue(true);
        }

        return true;
    }

    /**
     * parse string of a single-arg assignment. E.g. parse to type int, float, string, or bool
     *
     * @param argDesc: the argument descriptor
     * @param args: the tokenized string[] of arguments
     * @param argString: the tokenized argument string without the leading "-"/"--"
     * @param argIdx: the index of the argument denoted by argString
     * @param pd: the ref ParseData for this run
     * @returns true on success, false otherwise
     */
    private bool ParseSingleArg(ArgDescriptor argDesc, string[] args, string argString, 
            int argIdx, ref ParseData pd) {

        bool isOpt = (argDesc as OptionDescriptor != null);
        ArgData argData = (isOpt) ? pd.optionsOut[argString] : pd.positionalsOut[argString];

        //gather
        string[] primitiveArgs;
        bool ok = GatherArgs(args, argIdx, 1, ref pd, out primitiveArgs);
        if(!ok) {
            if(!isOpt || argData.SysType != typeof(bool))
                return false;
            else {
                argData.SetValue(true);
                return true;
            }
        }

        //set
        ok = TrySetValue(argData, primitiveArgs[0]);
        if(!ok) {
            if(!isOpt || argData.SysType != typeof(bool)){
                return false;
            }
            else {
                argData.SetValue(true);
                return true;
            }
        }

        //remove used arg
        pd.argIndexes.Remove(argIdx);
        return true;
    }

    /**
     * parse a well-defined array arg E.g. parse to type int[], float[], string[],
     * and the argument is "WellDefined" (MinArgs == MaxArgs && MinArgs>0)
     *
     * @param argDesc: the argument descriptor
     * @param args: the tokenized string[] of arguments
     * @param argString: the name to use to try to reference the argument (long or short name)
     * @param argIdx: the index of the argument denoted by argString
     * @param pd: the ref ParseData for this run
     * @returns true on success, false otherwise
     */
    private bool ParseWellDefinedArrayArg(ArgDescriptor argDesc, string[] args, 
            string argString, int argIdx, ref ParseData pd) {

        //get args
        int numArgs = argDesc.MaxArgs; //should be same as opt.MinArgs
        string[] arrayArgs;
        bool ok = GatherArgs(args, argIdx, numArgs, ref pd, out arrayArgs);
        if(!ok)
            return false;

        //set corresponding output data
        bool isOpt = (argDesc as OptionDescriptor != null);
        ArgData argDataOut = (isOpt) ? pd.optionsOut[argString] : pd.positionalsOut[argString];

        if(argDesc.AppendMode)
            ok = TryAppendValue(argDataOut, arrayArgs);
        else
            ok = TrySetValue(argDataOut, arrayArgs);
        
        //remove used args
        for(int i = argIdx; i < argIdx + numArgs; i++)
            pd.argIndexes.Remove(i);

        return ok;
    }

    /**
     * parse a "list" array arg of unkonw length E.g. parse to type int[], float[], string[],
     * and the argument is *not* "WellDefined" (MinArgs == MaxArgs && MinArgs>0)
     *
     * @param argDesc: the argument descriptor
     * @param args: the tokenized string[] of arguments
     * @param argString: the name to use to try to reference the argument (long or short name)
     * @param argIdx: the index of the argument denoted by argString
     * @param pd: the ref ParseData for this run
     * @returns true on success, false otherwise
     */
    private bool ParseListArg(ArgDescriptor argDesc, string[] args,
            string argString, int argIdx, ref ParseData pd) {

        int startIdx = argIdx;
        int endIdx = startIdx;

        int minArgs = argDesc.MinArgs;
        int maxArgs = argDesc.MaxArgs;
        bool behindDoubleDash = pd.doubleDashIdx > argIdx;

        while(endIdx < pd.maxArgIdx && 
                (!behindDoubleDash || !args[endIdx].StartsWith("-")) ) {
            endIdx++;
        }
        
        //check bounds on number of items in array
        int numArgs = endIdx - startIdx;
        if(maxArgs != -1 && numArgs > maxArgs) {
            Write("maximum array arguments exceeded");
            return false;
        }
        else if(minArgs != -1 && numArgs < minArgs) {
            Write("minimum array arguments not met.");
            return false;
        }
        
        //use our range to create args and set value
        List<string> listArgs = new List<string>();
        for(int i = startIdx; i < endIdx; i++) {
            listArgs.Add(args[i]);
            pd.argIndexes.Remove(i);
        }

        //try set
        bool isOpt = (argDesc as OptionDescriptor != null);
        ArgData argDataOut = (isOpt) ? pd.optionsOut[argString] : pd.positionalsOut[argString];

        bool ok;
        if(argDesc.AppendMode)
            ok = TryAppendValue(argDataOut, listArgs.ToArray());
        else
            ok = TrySetValue(argDataOut, listArgs.ToArray());

        return ok;
    }

    /**
     * parse all positional args and set the output data after args have
     * been categorized and optional arguments parsed out.
     *
     * @param args: the tokenized argument string
     * @param pd: the ref parse data for this parse run
     * @returns true on success, false otherwise.
     */
    private bool ParsePositionals(string[] args, ref ParseData pd) {

        foreach(ArgDescriptor posDesc in PosDescriptorsList) {

            //no more positionals left
            if(pd.argIndexes.Count == 0)
                break;

            ArgData posDataOut = pd.positionalsOut[posDesc.LongName];
            bool ok;

            //array-type
            if(posDataOut.SysType.IsArray) {
                if(posDesc.IsWellDefined) {
                    ok = ParseWellDefinedArrayArg(posDesc, args, 
                            posDesc.LongName, pd.argIndexes.First.Value, ref pd);
                }
                else { //guarenteed this is our last arg (based on addPositional checks)
                    ok = ParseListArg(posDesc, args, posDesc.LongName, pd.argIndexes.First.Value, ref pd);
                }
            }
            else{
                ok = ParseSingleArg(posDesc, args, posDesc.LongName, pd.argIndexes.First.Value, ref pd);
            }

            if(!ok)
                return false;
        }

        //extra args detected
        if(pd.argIndexes.Count != 0) {
            Write("Extra args detected");
            return false; 
        }
        //positions need a value, though defaults work too
        else {
            foreach(ArgData posData in pd.positionalsOut.Values.Distinct()){
                if(!posData.HasValue)
                    return false;
            }
        }

        return true;
    }

    /**
     * Takes a subcommand and lets the subcommand do its own parse run
     * for its arguments.
     *
     * @param args: the tokenized string array
     * @param pd: the ref ParseData for this run
     * @param result: the ParseResult to populate out (null on failure)
     * @returns true on success, false otherwise.
     */
    private bool ParseSubCommand(string[] args, 
            ref ParseData pd, out ParseResult result) {

        result = null;

        if(pd.subCommandIdx != -1) {
            Command subCommand = SubCommands[args[pd.subCommandIdx]];
            string[] subArgs = new string[args.Length - pd.subCommandIdx];
            Array.Copy(args, pd.subCommandIdx, subArgs, 0, subArgs.Length);

            ParseResult subResult;
            bool ok = subCommand.ParseArgs(subArgs, out subResult);
            if(!ok)
                return false;

            result = new ParseResult(pd.optionsOut, pd.positionalsOut, subCommand.Predicate, subResult);
        }
        else {
            result = new ParseResult(pd.optionsOut, pd.positionalsOut);
        }

        return true;
    }

    /**
     * tries to gather a requested number arguments, starting at some
     * index and working forward. Can fail if "-" encountered, end of
     * line, etc. So that's why we return the status as a bool after the fact.
     *
     * @param args: the tokenized argument array
     * @param argStartIdx: the (inclusive) index to start gathering arguments
     * @param numArgs: number of arguments to gather
     * @param pd: the ref ParseData for this run
     * @param outArgs: the out param of arguments gathered as strings.
     * @returns true on success, false otherwise.
     */
    private bool GatherArgs(string[] args, int argStartIdx, 
            int numArgs, ref ParseData pd, out string[] outArgs) {

        outArgs = null;
        bool behindDoubleDash = pd.doubleDashIdx > argStartIdx;

        //check arg amount met and validity
        for(int i = 0; i < numArgs; i++) {
            int argIdx = argStartIdx + i;
            if(argIdx >= pd.maxArgIdx || 
                (behindDoubleDash && args[argIdx].StartsWith("-")) ) {
                return false;
            }
        }
        
        outArgs = new string[numArgs];
        Array.Copy(args, argStartIdx, outArgs, 0, outArgs.Length);
        return true;
    }

    /******************************************************************
     * PARSING METHODS TO SET OR APPEND DATA
     ******************************************************************/

    /**
     * Tries to parse a string and set the data inside an ArgData.
     * The version that takes a single string only works for simple
     * types without array backing. The version where raw is a string[]
     * only works for array types.
     * 
     * @param data: the data to try to set
     * @param raw: the string to try and parse
     * @returns true on succesful parse, false otherwise
     */
    private bool TrySetValue(ArgData data, string raw) {

        bool ok;
        int intVal;
        float floatVal;

        switch(data.Type) {

            //int parse
            case ArgData.DataType.Int:
                ok = int.TryParse(raw, out intVal);
                if(ok)
                    data.SetValue(intVal);
                return ok;

            //no parse
            case ArgData.DataType.String:
                data.SetValue(raw);
                return true;

            //float parse
            case ArgData.DataType.Float:
                ok = float.TryParse(raw, out floatVal);
                if(ok)
                    data.SetValue(floatVal);

                return ok;
                
            //try to detect variety of positive strings
            case ArgData.DataType.Bool:
                raw = raw.ToLower();
                if(raw == "true" || raw == "t"
                    || raw == "y" || raw == "yes") {

                    data.SetValue(true);
                }
                else if(raw == "false" || raw == "f"
                    || raw == "n" || raw == "no"){

                    data.SetValue(false);
                }
                else
                    return false;

                return true;

        }

        return false;//shouldn't get here
    }

    /**
     * @param data: the data to try to set
     * @param raw: string[] to try and parse
     * @returns true on succesful parse, false otherwise
     */
    private bool TrySetValue(ArgData data, string[] raw) {

        switch(data.Type) {

            case ArgData.DataType.IntArray:
            {
                int[] intArrVal = new int[raw.Length];
                for(int i = 0; i < raw.Length; i++) {
                    int intVal;
                    bool ok = int.TryParse(raw[i], out intVal);
                    if(!ok)
                        return false;
                    else
                        intArrVal[i] = intVal;
                }
                data.SetValue(intArrVal);
                return true;
            }

            //no parsing required
            case ArgData.DataType.StringArray:
                data.SetValue(raw);
                return true;


            //try parse all as floats
            case ArgData.DataType.FloatArray:
            {
                float[] floatArrVal = new float[raw.Length];
                for(int i = 0; i < raw.Length; i++) {
                    float floatVal;
                    bool ok = float.TryParse(raw[i], out floatVal);
                    if(!ok)
                        return false;
                    else
                        floatArrVal[i] = floatVal;
                }
                
                data.SetValue(floatArrVal);
                return true;
            }

        }

        return false;
    }

    /**
     * Useful for setting the data of array-backed ArgData
     * with the caveat that it doesn't overwrite the old data, but rather
     * appends to it.
     *
     * @param data: the ArgData to append to
     * @param raw: the string to parse
     * @return true on succesful parse, false otherwise
     */
    private bool TryAppendValue(ArgData data, string raw) {

        switch(data.Type) {

            case ArgData.DataType.IntArray:
            {
                int[] existing = (int[])data.Value;
                int[] newArr = new int[existing.Length + 1];
                Array.Copy(existing, newArr, existing.Length);
                bool ok = int.TryParse(raw, out newArr[newArr.Length - 1]);
                if(ok)
                    data.SetValue(newArr);

                return ok;
            }

            case ArgData.DataType.StringArray:
            {
                string[] existing = (string[])data.Value;
                string[] newArr = new string[existing.Length + 1];
                Array.Copy(existing, newArr, existing.Length);
                newArr[newArr.Length - 1] = raw;
                data.SetValue(newArr);
                return true;
            }

            case ArgData.DataType.FloatArray:
            {
                float[] existing = (float[])data.Value;
                float[] newArr = new float[existing.Length + 1];
                Array.Copy(existing, newArr, existing.Length);
                bool ok = float.TryParse(raw, out newArr[newArr.Length - 1]);
                if(ok)
                    data.SetValue(newArr);

                return ok;
            }
        }

        return false;
    }

    private bool TryAppendValue(ArgData data, string[] raw) {

        switch(data.Type) {

            case ArgData.DataType.IntArray:
            {
                int[] existing = (int[])data.Value;
                int[] newArr = new int[existing.Length + raw.Length];
                Array.Copy(existing, newArr, existing.Length);

                for(int i = 0 ; i < raw.Length; i++) {
                    bool ok = int.TryParse(raw[i], out newArr[existing.Length + i]);
                    if(!ok)
                        return false;
                }
                data.SetValue(newArr);
                return true;
            }

            case ArgData.DataType.StringArray:
            {
                string[] existing = (string[])data.Value;
                string[] newArr = new string[existing.Length + 1];
                Array.Copy(existing, newArr, existing.Length);
                Array.Copy(raw, existing.Length, newArr, 0, raw.Length);
                data.SetValue(newArr);
                return true;
            }

            case ArgData.DataType.FloatArray:
            {
                float[] existing = (float[])data.Value;
                float[] newArr = new float[existing.Length + 1];
                Array.Copy(existing, newArr, existing.Length);

                for(int i = 0 ; i < raw.Length; i++) {
                    bool ok = float.TryParse(raw[i], out newArr[existing.Length + i]);
                    if(!ok)
                        return false;
                }
                data.SetValue(newArr);
                return true;
            }
        }

        return false;
    }


    /*************************************************************
     * WRITE CALLBACKS (for platform independent usage)
     *************************************************************/

    /**
     * Framework-independent way to write to the console.
     * Just give the top level parser a callback for onWrite,
     * and it will propogate it to all of its sub command parsers
     * as well. The following Write() function is just a safe way to
     * emit this event.
     * 
     * @param s: the string to write
     */
    public delegate void OnWriteHandler(string s);

    public event OnWriteHandler onWrite;
    public void Write(string s="") {
        OnWriteHandler handler = onWrite;
        if(handler != null)
            handler(s);
    }

    public event OnWriteHandler onWriteWarning;
    public void WriteWarning(string s="") {
        OnWriteHandler handler = onWriteWarning;
        if(handler != null)
            handler(s);
    }

    /**
     * A helper to choose the write help command to print
     */
    public void WriteHelp() {
        if(Formatter == null)
            Write(HelpTextFormatter.UnformattedHelp(this));
        else
            Write(Formatter.FormattedHelp(this));
    }
}

}
