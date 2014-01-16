/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Utilities
{
    /// <summary>
    /// Helper classses for dynamic property grid manipulations.
    /// Note: Following attribute can be helpful also: [System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
    /// </summary>
	class DynamicPropertyGridContainer: CollectionBase,ICustomTypeDescriptor
	{
		/// <summary>
		/// Add CustomProperty to Collectionbase List
		/// </summary>
		/// <param name="Value"></param>
		public void Add(CustomProperty Value)
		{
			base.List.Add(Value);
		}

		/// <summary>
		/// Remove item from List
		/// </summary>
		/// <param name="Name"></param>
		public void Remove(string Name)
		{
			foreach(CustomProperty prop in base.List)
			{
				if(prop.Name == Name)
				{
					base.List.Remove(prop);
					return;
				}
			}
		}

		/// <summary>
		/// Indexer
		/// </summary>
		public CustomProperty this[int index] 
		{
			get 
			{
				return (CustomProperty)base.List[index];
			}
			set
			{
				base.List[index] = (CustomProperty)value;
			}
		}

        public CustomProperty this[string name]
        {
            get
            {
                foreach (CustomProperty p in this)
                {
                    if (p.Name == name) return p;
                }
                return null;
            }
        }

		#region "TypeDescriptor Implementation"
		/// <summary>
		/// Get Class Name
		/// </summary>
		/// <returns>String</returns>
		public String GetClassName()
		{
			return TypeDescriptor.GetClassName(this,true);
		}

		/// <summary>
		/// GetAttributes
		/// </summary>
		/// <returns>AttributeCollection</returns>
		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this,true);
		}

		/// <summary>
		/// GetComponentName
		/// </summary>
		/// <returns>String</returns>
		public String GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		/// <summary>
		/// GetConverter
		/// </summary>
		/// <returns>TypeConverter</returns>
		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		/// <summary>
		/// GetDefaultEvent
		/// </summary>
		/// <returns>EventDescriptor</returns>
		public EventDescriptor GetDefaultEvent() 
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		/// <summary>
		/// GetDefaultProperty
		/// </summary>
		/// <returns>PropertyDescriptor</returns>
		public PropertyDescriptor GetDefaultProperty() 
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		/// <summary>
		/// GetEditor
		/// </summary>
		/// <param name="editorBaseType">editorBaseType</param>
		/// <returns>object</returns>
		public object GetEditor(Type editorBaseType) 
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		public EventDescriptorCollection GetEvents(Attribute[] attributes) 
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			PropertyDescriptor[] newProps = new PropertyDescriptor[this.Count];
			for (int i = 0; i < this.Count; i++)
			{
				CustomProperty  prop = (CustomProperty) this[i];
				newProps[i] = new CustomPropertyDescriptor(ref prop, attributes);
			}

			return new PropertyDescriptorCollection(newProps);
		}

		public PropertyDescriptorCollection GetProperties()
		{
			
			return TypeDescriptor.GetProperties(this, true);
			
		}

		public object GetPropertyOwner(PropertyDescriptor pd) 
		{
			return this;
		}
		#endregion

        public override string ToString()
        {
            return "Custom type";
        }
	}

	/// <summary>
	/// Custom property class 
	/// </summary>
	class CustomProperty
	{
		private string m_name = string.Empty;
		private bool m_readonly = false;
        private object m_old_value = null;
		private object m_value = null;
        private Type m_type;
        private object m_tag;
        private DynamicEnum m_options;
        private string m_category;
        private string m_description;

        public CustomProperty(string name, object value, Type type, bool read_only, string category = "", string description = "", DynamicEnum options = null, object tag = null)
		{
			this.m_name = name;
            this.m_old_value = value;
			this.m_value = value;
            this.m_type = type;
			this.m_readonly = read_only;
            this.m_tag = tag;
            this.m_options = options;
            this.m_category = category;
            this.m_description = description;
		}

        public DynamicEnum Options
        {
            get { return m_options; }
        }

        public Type Type
        {
            get { return m_type; }
        }

        public string Category
        {
            get { return m_category; }
        }

        public string Description
        {
            get { return m_description; }
        }

		public bool ReadOnly
		{
			get
			{
				return m_readonly;
			}
		}

		public string Name
		{
			get
			{
				return m_name;
			}
		}

		public bool Visible
		{
			get
			{
				return true;
			}
		}

		public object Value
		{
			get
			{
				return m_value;
			}
			set
			{
				m_value = value;
			}
		}

        public object Tag
        {
            get { return m_tag; }
        }

        public void Reset()
        {
            m_value = m_old_value;
        }
	}

    #region " DoubleConvert"

    /// <summary>
    /// A class to allow the conversion of doubles to string representations of
    /// their exact decimal values. The implementation aims for readability over
    /// efficiency.
    /// </summary>
    public class DoubleConverter
    {
        /// <summary>
        /// Converts the given double to a string representation of its
        /// exact decimal value.
        /// </summary>
        /// <param name="d">The double to convert.</param>
        /// <returns>A string representation of the double's exact decimal value.</return>
        public static string ToExactString(double d)
        {
            if (double.IsPositiveInfinity(d))
                return System.Globalization.NumberFormatInfo.CurrentInfo.PositiveInfinitySymbol;
            if (double.IsNegativeInfinity(d))
                return System.Globalization.NumberFormatInfo.CurrentInfo.NegativeInfinitySymbol;
            if (double.IsNaN(d))
                return System.Globalization.NumberFormatInfo.CurrentInfo.NaNSymbol;

            // Translate the double into sign, exponent and mantissa.
            long bits = BitConverter.DoubleToInt64Bits(d);
            // Note that the shift is sign-extended, hence the test against -1 not 1
            bool negative = (bits < 0);
            int exponent = (int)((bits >> 52) & 0x7ffL);
            long mantissa = bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                exponent++;
            }
            // Normal numbers; leave exponent as it is but add extra
            // bit to the front of the mantissa
            else
            {
                mantissa = mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            exponent -= 1075;

            if (mantissa == 0)
            {
                return "0";
            }

            /* Normalize */
            while ((mantissa & 1) == 0)
            {    /*  i.e., Mantissa is even */
                mantissa >>= 1;
                exponent++;
            }

            /// Construct a new decimal expansion with the mantissa
            ArbitraryDecimal ad = new ArbitraryDecimal(mantissa);

            // If the exponent is less than 0, we need to repeatedly
            // divide by 2 - which is the equivalent of multiplying
            // by 5 and dividing by 10.
            if (exponent < 0)
            {
                for (int i = 0; i < -exponent; i++)
                    ad.MultiplyBy(5);
                ad.Shift(-exponent);
            }
            // Otherwise, we need to repeatedly multiply by 2
            else
            {
                for (int i = 0; i < exponent; i++)
                    ad.MultiplyBy(2);
            }

            // Finally, return the string with an appropriate sign
            if (negative)
                return "-" + ad.ToString();
            else
                return ad.ToString();
        }

        /// <summary>Private class used for manipulating
        class ArbitraryDecimal
        {
            /// <summary>Digits in the decimal expansion, one byte per digit
            byte[] digits;
            /// <summary> 
            /// How many digits are *after* the decimal point
            /// </summary>
            int decimalPoint = 0;

            /// <summary> 
            /// Constructs an arbitrary decimal expansion from the given long.
            /// The long must not be negative.
            /// </summary>
            internal ArbitraryDecimal(long x)
            {
                string tmp = x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                digits = new byte[tmp.Length];
                for (int i = 0; i < tmp.Length; i++)
                    digits[i] = (byte)(tmp[i] - '0');
                Normalize();
            }

            /// <summary>
            /// Multiplies the current expansion by the given amount, which should
            /// only be 2 or 5.
            /// </summary>
            internal void MultiplyBy(int amount)
            {
                byte[] result = new byte[digits.Length + 1];
                for (int i = digits.Length - 1; i >= 0; i--)
                {
                    int resultDigit = digits[i] * amount + result[i + 1];
                    result[i] = (byte)(resultDigit / 10);
                    result[i + 1] = (byte)(resultDigit % 10);
                }
                if (result[0] != 0)
                {
                    digits = result;
                }
                else
                {
                    Array.Copy(result, 1, digits, 0, digits.Length);
                }
                Normalize();
            }

            /// <summary>
            /// Shifts the decimal point; a negative value makes
            /// the decimal expansion bigger (as fewer digits come after the
            /// decimal place) and a positive value makes the decimal
            /// expansion smaller.
            /// </summary>
            internal void Shift(int amount)
            {
                decimalPoint += amount;
            }

            /// <summary>
            /// Removes leading/trailing zeroes from the expansion.
            /// </summary>
            internal void Normalize()
            {
                int first;
                for (first = 0; first < digits.Length; first++)
                    if (digits[first] != 0)
                        break;
                int last;
                for (last = digits.Length - 1; last >= 0; last--)
                    if (digits[last] != 0)
                        break;

                if (first == 0 && last == digits.Length - 1)
                    return;

                byte[] tmp = new byte[last - first + 1];
                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = digits[i + first];

                decimalPoint -= digits.Length - (last + 1);
                digits = tmp;
            }

            /// <summary>
            /// Converts the value to a proper decimal string representation.
            /// </summary>
            public override String ToString()
            {
                char[] digitString = new char[digits.Length];
                for (int i = 0; i < digits.Length; i++)
                    digitString[i] = (char)(digits[i] + '0');

                // Simplest case - nothing after the decimal point,
                // and last real digit is non-zero, eg value=35
                if (decimalPoint == 0)
                {
                    return new string(digitString);
                }

                // Fairly simple case - nothing after the decimal
                // point, but some 0s to add, eg value=350
                if (decimalPoint < 0)
                {
                    return new string(digitString) +
                           new string('0', -decimalPoint);
                }

                // Nothing before the decimal point, eg 0.035
                if (decimalPoint >= digitString.Length)
                {
                    return "0" + System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator +
                        new string('0', (decimalPoint - digitString.Length)) +
                        new string(digitString);
                }

                // Most complicated case - part of the string comes
                // before the decimal point, part comes after it,
                // eg 3.5
                return new string(digitString, 0,
                                   digitString.Length - decimalPoint) +
                    System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator +
                    new string(digitString,
                                digitString.Length - decimalPoint,
                                decimalPoint);
            }
        }
    }

    #endregion

    public class DynamicEnum : ICollection
    {
        private Dictionary<string, int> m_stringIndex = new Dictionary<string, int>();
        private Dictionary<int, string> m_intIndex = new Dictionary<int, string>();
        public bool IsFlag { get; set; }

        public int this[string name]
        {
            get
            {
                int value = 0;

                if (name.IndexOf(',') != -1)
                {
                    int num = 0;
                    foreach (string str2 in name.Split(new char[] { ',' }))
                    {
                        m_stringIndex.TryGetValue(str2.Trim(), out value);
                        num |= value;
                    }
                    return num;
                }

                m_stringIndex.TryGetValue(name, out value);
                return value;
            }
        }
        public string this[int value]
        {
            get
            {
                if (IsFlag)
                {
                    string str = "";
                    foreach (KeyValuePair<string, int> entry in m_stringIndex)
                    {
                        if ((value & entry.Value) > 0 || (entry.Value == 0 && value == 0)) str += ", " + entry.Key;
                    }
                    if (str != "") str = str.Substring(2);
                    return str;
                }
                else
                {
                    string name;
                    m_intIndex.TryGetValue(value, out name);
                    return name;
                }
            }
        }
        public void Add(string name, int value)
        {
            m_stringIndex.Add(name, value);
            m_intIndex.Add(value, name);
        }
        public bool Contains(string name)
        {
            return m_stringIndex.ContainsKey(name);
        }
        public bool Contains(int value)
        {
            return m_intIndex.ContainsKey(value);
        }

        public IEnumerator GetEnumerator()
        {
            return m_stringIndex.GetEnumerator();
        }

        public int Count
        {
            get { return m_stringIndex.Count; }
        }

        public void CopyTo(Array array, int index)
        {
            int i = 0;
            foreach (KeyValuePair<string, int> entry in this)
                array.SetValue(entry, i++ + index);
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class DynamicEnumConverter : TypeConverter
    {
        // Fields
        private DynamicEnum m_e;

        public DynamicEnumConverter(DynamicEnum e)
        {
            m_e = e;
        }

        private static bool is_number(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length == 0) return false;
            for (int i = 0; i < str.Length; i++)
                if (!char.IsNumber(str, i)) return false;
            return true;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string && value != null)
            {
                string str = (string)value;
                str = str.Trim();

                if (m_e.Contains(str)) return m_e[str];
                else if (is_number(str))
                {
                    int int_val;
                    if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                        int_val = int.Parse(str.Substring(2), System.Globalization.NumberStyles.HexNumber);
                    else
                        int_val = int.Parse(str);
                    return int_val;
                }
                else
                {
                    return m_e[str];
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return true;
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            if ((destinationType == typeof(string)) && (value != null))
            {
                if (value is string)
                {
                    return value;
                }
                else if (value is KeyValuePair<string, int>)
                    return ((KeyValuePair<string, int>)value).Key;

                int val = (int)Convert.ChangeType(value, typeof(int));
                return m_e[val];
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new TypeConverter.StandardValuesCollection(m_e);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return !m_e.IsFlag;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            if (value is string) return m_e.Contains((string)value);

            int val = (int)Convert.ChangeType(value, typeof(int));
            return m_e.Contains(val);
        }
    }

    public class CustomSingleConverter : SingleConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if ((destinationType == typeof(string)) && (value.GetType() == typeof(float)))
            {
                return DoubleConverter.ToExactString((double)(float)value);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
         
    }

	/// <summary>
	/// Custom PropertyDescriptor
	/// </summary>
	class CustomPropertyDescriptor: PropertyDescriptor
	{
		CustomProperty m_Property;
		public CustomPropertyDescriptor(ref CustomProperty myProperty, Attribute [] attrs) :base(myProperty.Name, attrs)
		{
			m_Property = myProperty;
		}

        public CustomProperty CustomProperty
        {
            get { return m_Property; }
        }

		#region PropertyDescriptor specific
		
		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get 
			{
				return null;
			}
		}

		public override object GetValue(object component)
		{
			return m_Property.Value;
		}

		public override string Description
		{
			get
			{
				return m_Property.Description;
			}
		}
		
		public override string Category
		{
			get
			{
                return m_Property.Category;
			}
		}

		public override string DisplayName
		{
			get
			{
				return m_Property.Name;
			}
			
		}

		public override bool IsReadOnly
		{
			get
			{
				return m_Property.ReadOnly;
			}
		}

		public override void ResetValue(object component)
		{
            m_Property.Reset();
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}

		public override void SetValue(object component, object value)
		{
			m_Property.Value = value;
		}

        public override Type PropertyType
        {
            get { return m_Property.Type; }
        }

        public override TypeConverter Converter
        {
            get
            {
                if (m_Property.Type == typeof(DynamicPropertyGridContainer)) return new ExpandableObjectConverter();
                else if (m_Property.Options != null) return new DynamicEnumConverter(m_Property.Options);
                else if (m_Property.Type == typeof(float)) return new CustomSingleConverter();
                else return base.Converter;
            }
        }

        public DynamicEnum Options
        {
            get
            {
                return m_Property.Options;
            }
        }

		#endregion

			
	}

    public class IntToHexTypeDescriptionProvider : TypeDescriptionProvider
    {
        private IntToHexCustomTypeDescriptor m_Descriptor = new IntToHexCustomTypeDescriptor();

        public IntToHexTypeDescriptionProvider(TypeDescriptionProvider parent) :
            base(parent) { }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            if (objectType == typeof(int) || objectType == typeof(uint) || objectType == typeof(short) || objectType == typeof(ushort) || objectType == typeof(byte) || objectType == typeof(sbyte) || objectType == typeof(Int64) || objectType == typeof(UInt64))
            {
                return m_Descriptor;
            }
            else
            {
                return base.GetTypeDescriptor(objectType, instance);
            }
        }
    }

    public class IntToHexCustomTypeDescriptor : CustomTypeDescriptor
    {
        private IntToHexTypeConverter m_Converter = new IntToHexTypeConverter();
        public override TypeConverter GetConverter()
        {
            return m_Converter;
        }
    }

    public class IntToHexTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            else
            {
                return base.CanConvertFrom(context, sourceType);
            }
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }
            else
            {
                return base.CanConvertTo(context, destinationType);
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && (value.GetType() == typeof(byte) || value.GetType() == typeof(sbyte)))
            {
                return string.Format("0x{0:X2}", value);
            }
            else if (destinationType == typeof(string) && (value.GetType() == typeof(short) || value.GetType() == typeof(ushort)))
            {
                return string.Format("0x{0:X4}", value);
            }
            else if (destinationType == typeof(string) && (value.GetType() == typeof(int) || value.GetType() == typeof(uint)))
            {
                return string.Format("0x{0:X8}", value);
            }
            else if (destinationType == typeof(string) && (value.GetType() == typeof(Int64) || value.GetType() == typeof(UInt64)))
            {
                return string.Format("0x{0:X16}", value);
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(string))
            {
                string input = (string)value;

                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(2);
                }

                return base.ConvertFrom(context, culture, int.Parse(input, System.Globalization.NumberStyles.HexNumber, culture));
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
