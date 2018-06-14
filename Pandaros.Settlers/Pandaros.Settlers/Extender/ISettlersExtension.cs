﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pandaros.Settlers.Extender
{
    public interface ISettlersExtension
    {
        List<Type> LoadedAssembalies { get; }

        string InterfaceName { get; }

        void AfterWorldLoad();

        void AfterAddingBaseTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes);

        void AfterSelectedWorld();

        void AfterItemTypesDefined();
    }
}