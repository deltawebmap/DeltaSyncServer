﻿using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARKDinoDataReader.ARKProperties
{
    public abstract class BaseProperty
    {
        public string name;
        public string type;
        public int length;
        public int index;

        public static BaseProperty ReadProperty(IOMemoryStream ms)
        {
            //Read name
            string name;
            try
            {
                name = ms.ReadUEString();
                if (name == "None")
                    return null;
            } catch (Exception ex)
            {
                throw ex;
            }

            //Read type
            string type = ms.ReadUEString();
            if (name == "None")
                return null;

            //Create type
            BaseProperty p;
            switch(type)
            {
                case "FloatProperty": p = new FloatProperty(); break;
                case "BoolProperty": p = new BoolProperty(); break;
                case "IntProperty": p = new IntProperty(); break;
                case "ObjectProperty": p = new ObjectProperty(); break;
                case "StrProperty": p = new StrProperty(); break;
                case "UInt32Property": p = new UInt32Property(); break;
                case "ByteProperty": p = new ByteProperty(); break;
                case "DoubleProperty": p = new DoubleProperty(); break;
                case "UInt64Property": p = new UInt64Property(); break;
                case "UInt16Property": p = new UInt16Property(); break;
                case "ArrayProperty": p = new ArrayProperty(); break;
                case "Int8Property": p = new Int8Property(); break;
                case "StructProperty": p = new StructProperty(); break;
                default: throw new Exception("Unexpected type " + type + "!");
            }

            //Set values
            p.name = name;
            p.type = type;

            //Read additional parts
            p.length = ms.ReadInt();
            p.index = ms.ReadInt();

            //Now, read data
            p.Read(ms);

            return p;
        }

        public abstract void Read(IOMemoryStream ms);
        public virtual void Link(ARKDinoDataObject[] data)
        {

        }
    }
}
