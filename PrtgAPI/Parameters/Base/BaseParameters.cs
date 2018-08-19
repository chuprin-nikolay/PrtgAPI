﻿using System.Collections.Generic;
using PrtgAPI.Attributes;
using PrtgAPI.Helpers;
using PrtgAPI.Request;

namespace PrtgAPI.Parameters
{
    /// <summary>
    /// Represents parameters used to construct a <see cref="PrtgUrl"/>.
    /// </summary>
    public class BaseParameters : IParameters
    {
        bool IParameters.Cookie
        {
            get { return Cookie; }
            set { Cookie = value; }
        }

        internal bool Cookie { get; set; }

        private readonly Dictionary<Parameter, object> parameters = new Dictionary<Parameter, object>();

        /// <summary>
        /// Retrieves the underlying dictionary of parameters stored in this object.
        /// </summary>
        /// <returns>The underlying dictionary of parameters.</returns>
        public IDictionary<Parameter, object> GetParameters()
        {
            return parameters;
        }

        /// <summary>
        /// Gets or sets a <see cref="Parameter"/> for use in a <see cref="PrtgUrl"/>.
        /// </summary>
        /// <param name="parameter">The parameter to retrieve or modify.</param>
        /// <returns>The value of the specified parameter. If the parameter does not exist, null.</returns>
        public object this[Parameter parameter]
        {
            get
            {
                return parameters.ContainsKey(parameter) ? parameters[parameter] : null;
            }
            set
            {
                if (parameters.ContainsKey(parameter))
                {
                    parameters[parameter] = value;
                }
                else
                {
                    //If this parameter is mutually exclusive with another parameter (such as password/passhash),
                    //if our parameter's counterpart has already been added, replace it
                    var attrib = parameter.GetEnumAttribute<MutuallyExclusiveAttribute>();

                    if (attrib != null)
                    {
                        var counterpart = attrib.Name.ToEnum<Parameter>();

                        if (parameters.ContainsKey(counterpart))
                            parameters.Remove(counterpart);
                    }

                    parameters.Add(parameter, value);
                }
            }
        }
    }
}