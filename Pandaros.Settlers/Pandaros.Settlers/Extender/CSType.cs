﻿using Pipliz.JSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Pandaros.Settlers.Extender
{
    public abstract class CSType : ICSType
    {
        public virtual string Name { get; }
        public virtual bool isDestructible { get; } = true;
        public virtual bool isRotatable { get; } = false;
        public virtual bool isSolid { get; } = true;
        public virtual bool isFertile { get; } = false;
        public virtual bool isPlaceable { get; } = false;
        public virtual bool needsBase { get; } = false;
        public virtual int maxStackSize { get; } = 50;
        public virtual float nutritionalValue { get; } = 0f;
        public virtual string mesh { get; }
        public virtual string icon { get; }
        public virtual string onRemoveAudio { get; }
        public virtual string onPlaceAudio { get; }
        public virtual int destructionTime { get; } = 400;
        public virtual JSONNode customData { get; }
        public virtual string parentType { get; }
        public virtual string rotatablexp { get; }
        public virtual string rotatablexn { get; }
        public virtual string rotatablezp { get; }
        public virtual string rotatablezn { get; }
        public virtual string sideall { get; }
        public virtual string sidexp { get; }
        public virtual string sidexn { get; }
        public virtual string sideyp { get; }
        public virtual string sideyn { get; }
        public virtual string sidezp { get; }
        public virtual string sidezn { get; }
        public virtual Color color { get; } = Color.white;
        public virtual string onRemoveType { get; }
        public virtual string onRemoveAmount { get; }
        public virtual string onRemoveChance { get; }
        public virtual ReadOnlyCollection<OnRemove> onRemove { get; } = new ReadOnlyCollection<OnRemove>(new List<OnRemove>());
        public virtual bool blocksPathing => isSolid;
        public virtual ReadOnlyCollection<Colliders> colliders { get; } = new ReadOnlyCollection<Colliders>(new List<Colliders>());
        public virtual ReadOnlyCollection<string> categories { get; } = new ReadOnlyCollection<string>(new List<string>());

        public virtual JSONNode ToJsonNode()
        {
            var node = new JSONNode();

            node.SetAs(nameof(isDestructible), isDestructible);
            node.SetAs(nameof(isRotatable), isRotatable);
            node.SetAs(nameof(isSolid), isSolid);
            node.SetAs(nameof(isFertile), isFertile);
            node.SetAs(nameof(isPlaceable), isPlaceable);
            node.SetAs(nameof(needsBase), needsBase);
            node.SetAs(nameof(maxStackSize), maxStackSize);
            node.SetAs(nameof(nutritionalValue), nutritionalValue);
            node.SetAs(nameof(mesh), mesh);
            node.SetAs(nameof(icon), icon);
            node.SetAs(nameof(onRemoveAudio), onRemoveAudio);
            node.SetAs(nameof(destructionTime), destructionTime);
            node.SetAs(nameof(customData), customData);
            node.SetAs(nameof(parentType), parentType);
            node.SetAs(nameof(rotatablexp), rotatablexp);
            node.SetAs(nameof(rotatablexn), rotatablexn);
            node.SetAs(nameof(rotatablezp), rotatablezp);
            node.SetAs(nameof(rotatablezn), rotatablezn);
            node.SetAs(nameof(sideall), sideall);
            node.SetAs(nameof(sidexp), sidexp);
            node.SetAs(nameof(sidexn), sidexn);
            node.SetAs(nameof(sideyp), sideyp);
            node.SetAs(nameof(sideyn), sideyn);
            node.SetAs(nameof(sidezp), sidezp);
            node.SetAs(nameof(sidezn), sidezn);
            node.SetAs(nameof(color), color.ToRGBHex());
            node.SetAs(nameof(onRemoveType), onRemoveType);
            node.SetAs(nameof(onRemoveAmount), onRemoveAmount);
            node.SetAs(nameof(onRemoveChance), onRemoveChance);
            node.SetAs(nameof(onRemove), onRemove.ToJsonNode());
            node.SetAs(nameof(blocksPathing), blocksPathing);
            node.SetAs(nameof(colliders), colliders.ToJsonNode());
            node.SetAs(nameof(categories), categories.ToJsonNode());

            return node;    
        }
    }
}