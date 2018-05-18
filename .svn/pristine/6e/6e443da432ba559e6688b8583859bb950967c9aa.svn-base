using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.Builtin.tree
{
    public class tree : MC2Controller
    {
        #region Blocks

        public MC2Value first(DataTree datatree)
        {
            return ((DataTree)datatree)[0];
        }

        public MC2Value last(DataTree datatree)
        {
            return datatree[datatree.Length - 1];
        }

        public MC2Value getat(DataTree datatree, int location)
        {
            return datatree[(int)location];
        }

        /// <summary>
        /// Gets the child node of datatree with given name. Returns empty if provided name is empty.
        /// </summary>
        /// <param name="datatree"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public MC2Value get(DataTree datatree, string name)
        {
            if (string.IsNullOrEmpty(name))
                return MC2EmptyValue.EmptyValue;
            else
                return datatree[name];
        }

        public MC2Value getvalueordefault(DataTree datatree, string name, MC2Value defaultValue)
        {
            return datatree[name].GetValueOrDefault(defaultValue);
        }

        public MC2Value set(DataTree datatree, string name, MC2Value value)
        {
            datatree[name] = value;
            return MC2EmptyValue.EmptyValue;
        }

        public MC2Value clone(DataTree datatree)
        {
            return (DataTree)datatree.Clone();
        }

        public MC2Value addnodewithindex(DataTree datatree)
        {
            return datatree.AddNodeWithIndex();
        }

        public MC2Value length(DataTree datatree)
        {
            return datatree.Length;
        }

        public MC2Value parent(DataTree datatree)
        {
            return datatree.Parent;
        }

        public MC2Value contains(DataTree datatree, string name)
        {
            return datatree.Contains(name);
        }

        public MC2Value create()
        {
            return new DataTree();
        }

        #endregion
    }
}