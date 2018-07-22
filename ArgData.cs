using System;
using System.Collections.Generic;

/**
 * A class allowing us to store arbitrary data
 * in a serialized manner. Useful to flexibly store
 * command line arguments in their destination format,
 * with parse checking up front when parsing the command line.
 */
namespace cgParse {

public class ArgData {

    public enum DataType {
        Int,
        IntArray,
        String,
        StringArray,
        Float,
        FloatArray,
        Bool
    }

    public object Value{ get; protected set; }


    public bool HasValue{ get{ return (Value != null); } }
    public DataType Type{ get; private set; }   //useful in switch statements
    public System.Type SysType{                 //useful for getting type attributes of data
        get {
            switch(Type) {
                case DataType.Int:
                    return typeof(int);
                case DataType.IntArray:
                    return typeof(int[]);
                case DataType.String:
                    return typeof(string);
                case DataType.StringArray:
                    return typeof(string[]);
                case DataType.Float:
                    return typeof(float);
                case DataType.FloatArray:
                    return typeof(float[]);
                case DataType.Bool:
                    return typeof(bool);
                default:
                    return null; //we never get here
            }
        }
    }

    /**
     * Ctors provide us the ability to up-front define the value
     * we want from this object later on. If a specific value
     * is specified, this means you want to use the value as the
     * default. If the value is omitted, then we assume that
     * no data is held (in which case, HasValue returns false).
     */

    /**
     * @param dataType: the backing data type, but no data
     *  is stored
     */
    public ArgData(DataType dataType) {
        Type = dataType;
        Value = null;
    }

    /**
     * @param val: the value to take
     *  (int, int[], float, float[], string, string[], or bool)
     */
    public ArgData(int val) {
        SetValue(val);
    }
    public ArgData(int[] val) {
        SetValue(val);
    }
    public ArgData(string val) {
        SetValue(val);
    }
    public ArgData(string[] val) {
        SetValue(val);
    }
    public ArgData(float val) {
        SetValue(val);
    }
    public ArgData(float[] val) {
        SetValue(val);
    }
    public ArgData(bool val) {
        SetValue(val);
    }

    /**
     * A copy constructor
     */
    public ArgData(ArgData other) {
        this.Type = other.Type;
        this.Value = other.Value;
    }

    /*
     * @param val: the value of the arg.
     *  Takes any type that a constructor
     *  can take that sets a defautl value.
     *  (int, int[], float, float[], string, string[], or bool)
     */
    public void SetValue(int intVal){
        Type = DataType.Int;
        Value = intVal;
    }

    public void SetValue(int[] intArr){
        Type = DataType.IntArray;
        Value = intArr;
    }

    public void SetValue(string[] stringArr){
        Type = DataType.StringArray;
        Value = stringArr;
    }

    public void SetValue(string str){
        Type = DataType.String;
        Value = str;
    }
    
    public void SetValue(float floatVal){
        Type = DataType.Float;
        Value = floatVal;
    }

    public void SetValue(float[] floatArr){
        Type = DataType.FloatArray;
        Value = floatArr;
    }

    public void SetValue(bool boolVal){
        Type = DataType.Bool;
        Value = boolVal;
    }

    //implicit conversions for ease
    public static implicit operator int(ArgData data){
        return (int)data.Value;
    }
    public static implicit operator int[](ArgData data) {
        return (int[])data.Value;
    }
    public static implicit operator float(ArgData data) {
        return (float)data.Value;
    }
    public static implicit operator float[](ArgData data) {
        return (float[])data.Value;
    }
    public static implicit operator string(ArgData data) {
        return (string)data.Value;
    }
    public static implicit operator string[](ArgData data) {
        return (string[])data.Value;
    }
    public static implicit operator bool(ArgData data) {
        return (bool)data.Value;
    }
}

}
