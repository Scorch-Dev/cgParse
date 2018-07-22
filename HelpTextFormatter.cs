using System;
using System.Collections.Generic;
using System.Text;

/**
 * A helper class to format text output by our command parser, especially when
 * it comes to "help text" when the --help option is passed in.
 *
 * It works both in terminal (e.g. monospaced fonts and fixed column widths)
 * or in straight GUI environments (e.g. fonts that aren't always monospaced and have
 * widths in pixel counts rather than columns).
 *
 * There are also static methods in here for semi-formatted text when the number of columns
 * or terminal width is unkown, or there is no way to calculate the character width reliably.
 */
namespace cgParse {

public class HelpTextFormatter {

    private System.Func<string, int> StringWidth{ get; set; }
    private int NumColumns{ get; set; }

    /**
     * This constructor assumes that there is uneven character sizing, and/or
     * the width of your output window does not correspond 1:1 with the number
     * of characters in the window (e.g. one character does not take up one "width unit")
     *
     * @param numColumns: the number of "width units" total for your output window
     * @param stringWidth: a function object that takes a string, and allows
     *  us to calculate its width based on the string.
     */
    public HelpTextFormatter(int numColumns, System.Func<string,int> stringWidth) { 
        NumColumns = numColumns;
        StringWidth = stringWidth;
    }

    /**
     * A constructor that assumes each character is monospaced (same width), and
     * that the number of columns corresponds 1:1 with the number of characters
     * in a row. That is to say, that if the number of columsn is 5, then 5 characters
     * fit in that row. If this condition is met, than this constructor works perfectly.
     *
     * @param numColumns: the number of columns/characters that fit in a single line
     *  on the output window
     */
    public HelpTextFormatter(int numColumns) { 
        NumColumns = numColumns;
        StringWidth = (string s)=>{ return s.Length; };
    }


    //a helper for grouping help text data
    private struct Help{
        public string argHelp;
        public string help;
    }

    /**
     * Generates a string with a formatted help message (in unix-style
     * --help fashion). It calculates the formatting based on the
     *  number of columns and the function used to calculate string
     *  width (if provided in the constructor).
     *
     *  @param command: The cli command to print help text for
     *  @returns the formatted help string.
     */
    public string FormattedHelp(Command command) {
        
        //group together relevent help text and
        //precalcualate the biggest offset from left for wrapping text
        List<Help> optHelps = new List<Help>();
        List<Help> posHelps = new List<Help>();

        int maxArgHelpWidth = -1;
        foreach(OptionDescriptor opt in command.OptionalArgs) {
            Help help = new Help(); 
            help.argHelp = ArgHelp(opt);
            help.help = opt.HelpText;
            optHelps.Add(help);

            int argHelpWidth = StringWidth(help.argHelp);
            if(argHelpWidth > maxArgHelpWidth)
                maxArgHelpWidth = argHelpWidth;
        }
        foreach(ArgDescriptor pos in command.PositionalArgs) {
            Help help = new Help(); 
            help.argHelp = ArgHelp(pos);
            help.help = pos.HelpText;
            posHelps.Add(help);

            int argHelpWidth = StringWidth(help.argHelp);
            if(argHelpWidth > maxArgHelpWidth)
                maxArgHelpWidth = argHelpWidth;
        }

        //calculate widths for wrapping "arghelp" text and "help" text
        if(maxArgHelpWidth > (NumColumns * 3 / 8))
            maxArgHelpWidth = NumColumns * 3 / 8;

        int helpTextWidth = (int)((NumColumns - maxArgHelpWidth) * 0.85f);
        int helpTextOffset = NumColumns - helpTextWidth - maxArgHelpWidth;

        //begin writing, then wrap and crowd our arg help texts
        StringBuilder sb = new StringBuilder(400);
        sb.Append(Usage(command));

        sb.Append("\n\nPositional arguments\n"
            + "------------------------\n");
        foreach(Help h in posHelps) {
            sb.Append(WrapAndCrowd(h.argHelp, maxArgHelpWidth, h.help, helpTextWidth, helpTextOffset));
            sb.Append('\n');
        }

        sb.Append("\nOptional arguments\n"
            + "------------------------\n");
        foreach(Help h in optHelps) {
            sb.Append(WrapAndCrowd(h.argHelp, maxArgHelpWidth, h.help, helpTextWidth, helpTextOffset));
            sb.Append('\n');
        }

        return sb.ToString();
    }

    /**
     * Wrap and crowd is another way of saying to take two long, paragraph strings, then
     * to turn these two strings into formatted text that looks like two paragraphs next to
     * each other (like in a newspaper).
     *
     * @param left: the left string to operate on (shows up on the left at the end)
     * @param leftWidth: the width of the left column in the outpupt.
     * @param right: the right string to operate on (shows up on the right at the end)
     * @param rightWidth: the width of the right column in the output.
     * @param spacing: the spacing between the generated columns
     * @returns a formatted string
     */
    private string WrapAndCrowd(string left, int leftWidth, string right, int rightWidth, int spacing) {

        //wrap arrays and pad to the same length
        string[] leftLines = WrapStringByWidth(left, leftWidth);
        string[] rightLines = WrapStringByWidth(right, rightWidth);
        if(rightLines.Length > leftLines.Length) {
            Array.Resize<string>(ref leftLines, rightLines.Length);
            for(int i = 0; i < leftLines.Length; i++){
                if(leftLines[i] == null)
                    leftLines[i] = "";
            }
        }
        else if(leftLines.Length > rightLines.Length) {
            Array.Resize<string>(ref rightLines, leftLines.Length);
            for(int i = 0; i < rightLines.Length; i++){
                if(rightLines[i] == null)
                    rightLines[i] = "";
            }
        }

        //split help into lines
        StringBuilder sb = new StringBuilder(NumColumns * leftLines.Length);
        for(int i = 0; i < leftLines.Length; i++) {
            string leftLine = leftLines[i];
            string rightLine = rightLines[i];
            int thisLineSpacing = (leftWidth - StringWidth(leftLine)) + spacing;
            string crowded = CrowdLine(leftLine, rightLine, thisLineSpacing);
            sb.Append(crowded);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /**
     * Crowding refers to taking two lines, and sticking them on one line,
     * with some constant offset of the right from the left side of the screen.
     * The only caveat is the the string that goes on the left must be shorter
     * than that absolute offset specified.
     *
     * @param left: the string that appears on hte left
     * @param right: the string to appear on the right
     * @param spacing: the amount of whitespace in width units to append
     *      between the strings.
     * @returns a single crowded string with the two strings and requested spacing
     */
    private string CrowdLine(string left, string right, int spacing) {

        StringBuilder sb = new StringBuilder(NumColumns);
        sb.Append(left);

        if(right.Length != 0) { //no visual difference
            int whitespaceWidth = StringWidth(" ");
            int bufferCharsNeeded = spacing / whitespaceWidth;
            for(int i = 0; i < bufferCharsNeeded; i++)
                sb.Append(' ');

            sb.Append(right);
        }

        return sb.ToString();
    }

    /**
     * Wraps a string by splitting it into multiple lines of
     * width <= the width parameter specified. Wrapped lines
     * are left-adjusted.
     *
     * @param s: the string to wrap
     * @param width: the width of the column to wrap the string around
     * @returns a string[] where each entry is a line in the wrapped
     *  string.
     */
    private string[] WrapStringByWidth(string s, int width) {

        string[] words = s.Split(' ');
        List<string> wrappedLines = new List<string>();
        StringBuilder lineBuilder = new StringBuilder(200);

        int whitespaceWidth = StringWidth(" ");
        int lineWidth = 0;

        foreach(string word in words){
            
            int wordWidth = StringWidth(word);
            if( (lineWidth + wordWidth) > width ) {

                wrappedLines.Add(lineBuilder.ToString());
                lineBuilder.Length = 0; //Clear() method is only in .NET 4.0
                lineWidth = 0;
            }

            lineBuilder.Append(word);
            lineBuilder.Append(' ');
            lineWidth += wordWidth + whitespaceWidth;
        }
        wrappedLines.Add(lineBuilder.ToString());

        return wrappedLines.ToArray();
    }

    /**
     * Generates an unformatted (actually semi-formatted) help string for when
     * no formatter is present or if no formatting is required.
     *
     * The semi-formatted string just prints argument help and the associated 
     * help text on a new line instead of trying to wrap it to side-by-side columns.
     *
     * @param command: the command to generate a help string for.
     */
    public static string UnformattedHelp(Command command) {
        StringBuilder sb = new StringBuilder();
        sb.Append(Usage(command));

        sb.Append("\n\nPositional arguments\n"
            + "------------------------\n");
        foreach(ArgDescriptor pos in command.PositionalArgs) {
            sb.Append(ArgHelp(pos));
            if(pos.HelpText.Length > 0) {
                sb.Append('\n');
                sb.Append(pos.HelpText);
            }
            sb.Append("\n\n");
        }

        sb.Append("\nOptional arguments\n"
            + "------------------------\n");
        foreach(OptionDescriptor opt in command.OptionalArgs) {
            sb.Append(ArgHelp(opt));
            if(opt.HelpText.Length > 0) {
                sb.Append('\n');
                sb.Append(opt.HelpText);
            }
            sb.Append("\n\n");
        }

        return sb.ToString();
    }

    /**
     * A helper that prints out the general help string for the usage
     * of a single argument/option. This
     * generally takes the form of "--option|-optShortName OPTION"
     * for options or "ARGUMENT_NAME" for positional arguments.
     *
     * Array arguments will have append indexes after the arugment
     * like "ARGUMENT_NAME_0 ARGUMENT_NAME_1" and may print an
     * "..." if there is no maximum number of arguments.
     *
     * @param arg: the arg to use to generate the arg help
     * @returns an arg help string
     */
    private static string ArgHelp(ArgDescriptor arg) {

        StringBuilder sb = new StringBuilder(200);

        //print name first
        OptionDescriptor asOpt = arg as OptionDescriptor;
        if(asOpt != null && asOpt.ShortName != ' ')
            sb.Append(string.Format("--{0}|-{1}", asOpt.LongName, asOpt.ShortName));
        else if(asOpt != null)
            sb.Append(string.Format("--{0}", arg.LongName));

        //arg array
        if(arg.Data.SysType.IsArray) {
            
            //well formed is easy
            if(arg.IsWellDefined) {
                for(int i = 0; i < arg.MinArgs; i++)
                    sb.Append(string.Format(" {0}_{1}", arg.LongName.ToUpper(), i));
            }
            //non-well-formed requires more care to print min args, then ... if no max, or parenthesized max
            else {
                int min = arg.MinArgs;
                int max = arg.MaxArgs;
                for(int i = 0; i < min; i++)
                    sb.Append(string.Format(" {0}_{1}", arg.LongName.ToUpper(), i));

                if(max >= 0) {
                    for(int i = 0; i < max-min; i++)
                        sb.Append(string.Format(" {0}_{1}", arg.LongName.ToUpper(), i));
                }
                else {
                    sb.Append(" ...");
                }
            }
        }
        //bools don't have to print the argument (but they can take one)
        else if(arg.Data.SysType != typeof(bool))
            sb.Append(string.Format(" {0}", arg.LongName.ToUpper()));


        return sb.ToString();
    }

    /**
     * Generates a uasge string for a command.
     * This generally lookos like:
     * "Usage: predicate <arg_help_1> <arg_help_2> ..."
     *
     * See the ArgHelp() function for the format of these <arg_help>
     * outputs.
     *
     * @param cmd: the cli command to use to generate the usage string
     * @returns the usage string
     */
    public static string Usage(Command cmd) {

        StringBuilder sb = new StringBuilder(200);
        sb.Append("usage: ");
        sb.Append(cmd.Predicate);

        foreach(OptionDescriptor opt in cmd.OptionalArgs) {
            sb.Append(" [");
            sb.Append(ArgHelp(opt));
            sb.Append("]");
        }
        foreach(ArgDescriptor pos in cmd.PositionalArgs) {
            sb.Append(' ');
            sb.Append(ArgHelp(pos));
        }
        return sb.ToString();
    }
}

}
