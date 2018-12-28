namespace PaperGLTF
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using GLTF.Schema;
    using UnityEngine;
    using UnityGLTF.Extensions;
    using PaperGLTF.Schema;

    using Egret3DExportTools;

    public class MaterialWriter : GLTFExporter
    {
        enum BlendMode
        {
            None,
            Blend,
            Blend_PreMultiply,
            Add,
            Add_PreMultiply,
            Subtractive,
            Subtractive_PreMultiply,
            Multiply,
            Multiply_PreMultiply,
        }

        enum EnableState
        {
            BLEND = 3042,
            CULL_FACE = 2884,
            DEPTH_TEST = 2929,
            POLYGON_OFFSET_FILL = 32823,
            SAMPLE_ALPHA_TO_COVERAGE = 32926,
        }

        enum BlendEquation
        {
            FUNC_ADD = 32774,
            FUNC_SUBTRACT = 32778,
            FUNC_REVERSE_SUBTRACT = 32779,
        }

        enum BlendFactor
        {
            ZERO = 0,
            ONE = 1,
            SRC_COLOR = 768,
            ONE_MINUS_SRC_COLOR = 769,
            DST_COLOR = 774,
            ONE_MINUS_DST_COLOR = 775,
            SRC_ALPHA = 770,
            ONE_MINUS_SRC_ALPHA = 771,
            DST_ALPHA = 772,
            ONE_MINUS_DST_ALPHA = 773,
            CONSTANT_COLOR = 32769,
            ONE_MINUS_CONSTANT_COLOR = 32770,
            CONSTANT_ALPHA = 32771,
            ONE_MINUS_CONSTANT_ALPHA = 32772,
            SRC_ALPHA_SATURATE = 776,
        }

        enum CullFace
        {
            FRONT = 1028,
            BACK = 1029,
            FRONT_AND_BACK = 1032,
        }

        enum FrontFace
        {
            CW = 2304,
            CCW = 2305,
        }

        enum DepthFunc
        {
            NEVER = 512,
            LESS = 513,
            LEQUAL = 515,
            EQUAL = 514,
            GREATER = 516,
            NOTEQUAL = 517,
            GEQUAL = 518,
            ALWAYS = 519,
        }

        private bool _isParticle = false;
        private bool _isAnimation = false;
        private UnityEngine.Material _target;
        public MaterialWriter(UnityEngine.Material target, bool isParticle, bool isAnimation = false) : base()
        {
            this._target = target;
            this._isParticle = isParticle;
            this._isAnimation = isAnimation;
        }

        protected override void Init()
        {
            base.Init();

            this._root = new GLTFRoot
            {
                Accessors = new List<Accessor>(),
                Asset = new Asset
                {
                    Version = "2.0",
                    Generator = "paper",
                    Extensions = new Dictionary<string, IExtension>(),
                },
                Materials = new List<GLTF.Schema.Material>(),
            };
        }

        public float GetFloat(string key, float defalutValue)
        {
            if (this._target.HasProperty(key))
            {
                return this._target.GetFloat(key);
            }

            return defalutValue;
        }

        public UnityEngine.Color GetColor(string key, UnityEngine.Color defalutValue)
        {
            if (this._target.HasProperty(key))
            {
                return this._target.GetColor(key);
            }

            return defalutValue;
        }

        public UnityEngine.Vector4 GetVector4(string key, UnityEngine.Vector4 defalutValue)
        {
            if (this._target.HasProperty(key))
            {
                return this._target.GetVector(key);
            }

            return defalutValue;
        }

        public UnityEngine.Texture GetTexture(string key, UnityEngine.Texture defalutValue)
        {
            if (this._target.HasProperty(key))
            {
                return this._target.GetTexture(key);
            }

            return defalutValue;
        }

        private string FindShaderName(bool isTextureEmpty = false)
        {
            var target = this._target;
            var renderType = target.GetTag("RenderType", false, "");
            var shaderName = target.shader.name.ToLower();
            var isBlend = shaderName.Contains("blended");
            var isAdditive = shaderName.Contains("additive");
            var isMultiply = shaderName.Contains("multiply");
            var isDoubleSide = target.HasProperty("_Cull") && target.GetInt("_Cull") == (int)UnityEngine.Rendering.CullMode.Off;
            if (!isDoubleSide)
            {
                isDoubleSide = shaderName.Contains("both") || shaderName.Contains("side");
            }
            var isPremultiply = shaderName.Contains("premultiply");
            var lightType = ExportToolsSetting.instance.lightType;

            if (this._isParticle)
            {
                if (isBlend)
                {
                    return isPremultiply ? "particle_blend_premultiply" : "particle_blend";
                }
                else if (isAdditive)
                {
                    return isPremultiply ? "particle_additive_premultiply" : "particle_additive";
                }
                else if (isMultiply)
                {
                    return isPremultiply ? "particle_multiply_premultiply" : "particle_multiply";
                }
                else
                {
                    return "particle";
                }
            }
            else if (shaderName.Contains("color") || isTextureEmpty)
            {
                if (renderType == "Transparent")
                {
                    if (isAdditive)
                    {
                        return "transparent_additive_color";
                    }
                    else
                    {
                        return "transparent_color";
                    }
                }
                return "materialcolor";
            }
            else
            {
                if (renderType == "Transparent")
                {
                    if (isAdditive)
                    {
                        return isDoubleSide ? "transparent_additive_doubleside" : "transparent_additive";
                    }
                    else if (isMultiply)
                    {
                        return isDoubleSide ? "transparent_multiply_doubleside" : "transparent_multiply";
                    }
                    else
                    {
                        return isDoubleSide ? "transparent_doubleside" : "transparent";
                    }
                }
                else
                {
                    if (isDoubleSide)
                    {
                        // if (lightType == ExportLightType.Physical)
                        // {
                        //     return "meshphysical_doubleside";
                        // }
                        if (lightType == ExportLightType.Phong)
                        {
                            return "meshphong_doubleside";
                        }
                        if (lightType == ExportLightType.Lambert)
                        {
                            return "meshlambert_doubleside";
                        }
                        return "meshbasic_doubleside";
                    }
                    else
                    {
                        // if (lightType == ExportLightType.Physical)
                        // {
                        //     return "meshphysical";
                        // }
                        if (lightType == ExportLightType.Phong)
                        {
                            return "meshphong";
                        }
                        if (lightType == ExportLightType.Lambert)
                        {
                            return "meshlambert";
                        }
                        return "meshbasic";
                    }
                }
            }
        }

        public override byte[] WriteGLTF()
        {
            var target = this._target;

            var isCustom = ExportConfig.instance.IsCustomShader(target.shader.name);

            var materialJson = new MyJson_Tree();

            var assetJson = new MyJson_Tree();
            assetJson.SetInt("version", 2);
            materialJson.Add("asset", assetJson);

            var materialsJson = new MyJson_Array();
            materialJson.Add("materials", materialsJson);

            var extensionsRequired = new MyJson_Array();
            extensionsRequired.AddString("KHR_techniques_webgl");
            extensionsRequired.AddString("paper");
            materialJson.Add("extensionsRequired", extensionsRequired);

            var extensionsUsed = new MyJson_Array();
            extensionsUsed.AddString("KHR_techniques_webgl");
            extensionsUsed.AddString("paper");
            materialJson.Add("extensionsUsed", extensionsUsed);

            var materialItemJson = new MyJson_Tree();
            materialsJson.Add(materialItemJson);
            var extensions = new MyJson_Tree();
            materialItemJson.Add("extensions", extensions);

            var KHR_techniques_webglJson = new MyJson_Tree();
            extensions.Add("KHR_techniques_webgl", KHR_techniques_webglJson);

            var paperJson = new MyJson_Tree();
            var definesJson = new MyJson_Array();
            paperJson.SetInt("renderQueue", target.renderQueue);
            paperJson.Add("defines", definesJson);
            extensions.Add("paper", paperJson);

            var valuesJson = new MyJson_Tree();
            KHR_techniques_webglJson.Add("values", valuesJson);

            UnityEditor.MaterialProperty[] orginmps = UnityEditor.MaterialEditor.GetMaterialProperties(new UnityEngine.Object[] { target });
            if (!isCustom)
            {
                var color = Color.white;
                UnityEngine.Texture texture = null;
                if (target.HasProperty("_Color"))
                {
                    color = target.GetColor("_Color");
                }
                else if (target.HasProperty("_MainColor"))
                {
                    color = target.GetColor("_MainColor");
                }
                else if (target.HasProperty("_TintColor"))
                {
                    color = target.GetColor("_TintColor");
                }

                foreach (var mp in orginmps)
                {
                    Debug.Log("材质名字:" + mp.name + " 类型:" + mp.type);
                    if (mp.type.ToString() == "Texture")
                    {
                        var t = target.GetTexture(mp.name);
                        if(t)
                        {
                            Debug.Log("图片:" + t.name);
                        }                        
                    }
                }

                //TODO 寻找第一个Texture
                foreach (var mp in orginmps)
                {
                    if (mp.type.ToString() == "Texture")
                    {
                        var tex = target.GetTexture(mp.name);
                        if (tex != null)
                        {
                            texture = tex;
                            break;
                        }
                    }
                }

                valuesJson.SetVector3("diffuse", new Vector3(color.r, color.g, color.b));
                valuesJson.SetNumber("opacity", color.a);
                if (!this._isParticle)
                {
                    if (ExportToolsSetting.instance.lightType == ExportLightType.Phong)
                    {
                        if (target.HasProperty("_SpecGlossMap"))
                        {
                            var specularMap = target.GetTexture("_SpecGlossMap");
                            if (specularMap != null)
                            {
                                var texPath = ResourceManager.instance.SaveTexture(specularMap as Texture2D, "");
                                valuesJson.SetString("specularMap", texPath);
                                definesJson.AddString("USE_SPECULARMAP");
                            }
                        }

                        if (target.HasProperty("_BumpMap"))
                        {
                            var normalMap = target.GetTexture("_BumpMap");
                            if (normalMap != null)
                            {
                                var texPath = ResourceManager.instance.SaveTexture(normalMap as Texture2D, "");
                                valuesJson.SetString("normalMap", texPath);
                                definesJson.AddString("USE_NORMALMAP");
                            }
                        }
                    }
                    // else if (ExportToolsSetting.instance.lightType == ExportLightType.Physical)
                    // {
                    //     var roughness = 1.0f - this.GetFloat("_Glossiness", 0.0f);
                    //     var metalness = this.GetFloat("_Metallic", 0.0f);
                    //     var aoMap = this.GetTexture("_OcclusionMap", null);
                    //     var aoMapIntensity = this.GetFloat("_OcclusionStrength", 0.0f);
                    //     var emissive = this.GetColor("_EmissionColor", Color.black);
                    //     var emissiveMap = this.GetTexture("_EmissionMap", null);
                    //     var emissiveIntensity = 1.0f;
                    //     var bumpMap = this.GetTexture("_BumpMap", null);
                    //     var bumpScale = this.GetFloat("_BumpScale", 1.0f);
                    //     var normalMap = this.GetTexture("_DetailNormalMap", null);
                    //     var normalScale = this.GetFloat("_DetailNormalMapScale", 0.0f);
                    //     UnityEngine.Texture roughnessMap = null;
                    //     var metalnessMap = this.GetTexture("_MetallicGlossMap", null);
                    //     var displacementMap = this.GetTexture("_ParallaxMap", null);
                    //     var displacementScale = this.GetFloat("_Parallax", 0.0f);

                    //     valuesJson.SetColor3("emissive", emissive);
                    //     valuesJson.SetNumber("roughness", roughness);
                    //     valuesJson.SetNumber("metalness", metalness);

                    //     if (aoMap != null)
                    //     {
                    //         var texPath = ResourceManager.instance.SaveTexture(aoMap as Texture2D, "");
                    //         valuesJson.SetString("aoMap", texPath);
                    //         valuesJson.SetNumber("aoMapIntensity", aoMapIntensity);

                    //         definesJson.AddString("USE_AOMAP");
                    //     }

                    //     if (emissiveMap != null)
                    //     {
                    //         var texPath = ResourceManager.instance.SaveTexture(emissiveMap as Texture2D, "");
                    //         valuesJson.SetString("emissiveMap", texPath);
                    //         definesJson.AddString("USE_EMISSIVEMAP");
                    //     }

                    //     if (bumpMap != null)
                    //     {
                    //         var texPath = ResourceManager.instance.SaveTexture(bumpMap as Texture2D, "");
                    //         // valuesJson.SetString("bumpMap", texPath);
                    //         // valuesJson.SetNumber("bumpScale", bumpScale / 10.0f);
                    //         // definesJson.AddString("USE_BUMPMAP");
                    //         valuesJson.SetString("normalMap", texPath);
                    //         valuesJson.SetVector2("normalScale", new UnityEngine.Vector2(bumpScale, bumpScale));
                    //         definesJson.AddString("USE_NORMALMAP");
                    //     }

                    //     if (normalMap != null)
                    //     {
                    //         var texPath = ResourceManager.instance.SaveTexture(normalMap as Texture2D, "");
                    //         valuesJson.SetString("normalMap", texPath);
                    //         valuesJson.SetVector2("normalScale", new UnityEngine.Vector2(normalScale / 10.0f, normalScale / 10.0f));
                    //         definesJson.AddString("USE_NORMALMAP");
                    //     }

                    //     if(displacementMap != null)
                    //     {
                    //         var texPath = ResourceManager.instance.SaveTexture(displacementMap as Texture2D, "");
                    //         valuesJson.SetString("displacementMap", texPath);
                    //         valuesJson.SetNumber("displacementScale", displacementScale);
                    //         valuesJson.SetNumber("displacementBias", 0.0f);
                    //         definesJson.AddString("USE_DISPLACEMENTMAP");
                    //     }
                    // }
                }

                if (texture != null)
                {
                    var texPath = ResourceManager.instance.SaveTexture(texture as Texture2D, "");
                    valuesJson.SetString("map", texPath);

                    var mainST = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                    MyJson_Array uvTransform = new MyJson_Array();
                    if (target.HasProperty("_MainTex_ST"))
                    {
                        mainST = target.GetVector("_MainTex_ST");
                    }
                    var tx = mainST.z;
                    var ty = mainST.w;
                    var sx = mainST.x;
                    var sy = mainST.y;
                    var cx = 0.0f;
                    var cy = 0.0f;
                    var rotation = 0.0f;
                    var c = Math.Cos(rotation);
                    var s = Math.Sin(rotation);

                    uvTransform.AddNumber(sx * c);
                    uvTransform.AddNumber(sx * s);
                    uvTransform.AddNumber(-sx * (c * cx + s * cy) + cx + tx);
                    uvTransform.AddNumber(-sy * s);
                    uvTransform.AddNumber(sy * c);
                    uvTransform.AddNumber(-sy * (-s * cx + c * cy) + cy + ty);
                    uvTransform.AddNumber(0.0);
                    uvTransform.AddNumber(0.0);
                    uvTransform.AddNumber(1.0);

                    valuesJson.Add("uvTransform", uvTransform);
                    if (!_isParticle)
                    {
                        definesJson.AddString("USE_MAP");
                    }
                }
                else
                {
                    MyLog.LogWarning("纯色材质:" + target.name);
                }

                if (target.HasProperty("_Cutoff"))
                {
                    var cutoff = target.GetFloat("_Cutoff");
                    if (cutoff < 1.0f)
                    {
                        definesJson.AddString("ALPHATEST " + cutoff);
                    }
                }

                var shaderName = FindShaderName(texture == null);
                KHR_techniques_webglJson.SetString("technique", "builtin/" + shaderName + ".shader.json");
            }
            else
            {
                var shaderName = target.shader.name;
                foreach (var mp in orginmps)
                {
                    if (mp.flags == UnityEditor.MaterialProperty.PropFlags.HideInInspector)
                        continue;

                    var _uniform = new MyJson_Tree();
                    string type = mp.type.ToString();
                    if (type == "Float" || type == "Range")
                    {
                        valuesJson.SetNumber(mp.name, target.GetFloat(mp.name));
                    }
                    else if (type == "Vector")
                    {
                        valuesJson.SetVector4(mp.name, target.GetVector(mp.name));
                    }
                    else if (type == "Color")
                    {
                        valuesJson.SetColor(mp.name, target.GetColor(mp.name));
                    }
                    else if (type == "Texture")
                    {
                        //Debug.Log(mp.textureValue + "_:" + mp.textureDimension.ToString());
                        string texdim = mp.textureDimension.ToString();
                        var tex = target.GetTexture(mp.name);
                        if (tex != null)
                        {
                            if (texdim == "Tex2D")
                            {
                                var texPath = ResourceManager.instance.SaveTexture(tex as Texture2D, "");
                                valuesJson.SetString(mp.name, texPath);

                                string propertyName = mp.name + "_ST";
                                if (target.HasProperty(propertyName))
                                {
                                    valuesJson.SetVector4(propertyName, target.GetVector(propertyName));
                                }
                            }
                            else
                            {
                                throw new Exception("not suport texdim:" + texdim);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("not support type: " + mp.type);
                    }
                }

                var statesJson = new MyJson_Tree();
                paperJson.Add("states", statesJson);
                var enalbesJson = new MyJson_Array();
                var functionsJson = new MyJson_Tree();
                statesJson.Add("enable", enalbesJson);
                statesJson.Add("functions", functionsJson);

                if (target.GetTag("RenderType", false, "") == "Transparent")
                {
                    var isBlend = shaderName.Contains("blended");
                    var isAdditive = shaderName.Contains("additive");
                    var isMultiply = shaderName.Contains("multiply");
                    this.SetBlend(enalbesJson, functionsJson, BlendMode.Blend);
                }

                var doubleSided = target.HasProperty("_Cull") && target.GetInt("_Cull") == (float)UnityEngine.Rendering.CullMode.Off;
                if (!doubleSided)
                {
                    doubleSided = shaderName.Contains("both") || shaderName.Contains("side");
                }
                if (doubleSided)
                {
                    this.SetCullFace(enalbesJson, functionsJson, false, FrontFace.CCW, CullFace.BACK);
                }
                else
                {
                    this.SetCullFace(enalbesJson, functionsJson, true, FrontFace.CCW, CullFace.BACK);
                }

                this.SetDepth(enalbesJson, functionsJson, true, true);

                var url = UnityEditor.AssetDatabase.GetAssetPath(target.shader);
                url += ".json";
                KHR_techniques_webglJson.SetString("technique", url);
                MyLog.Log("自定义Shader:" + url);
            }


            materialJson.SetInt("version", 4);

            // var statesJson = new MyJson_Tree();
            // paperJson.Add("states", statesJson);
            // var enalbesJson = new MyJson_Array();
            // var functionsJson = new MyJson_Tree();
            // statesJson.Add("enable", enalbesJson);
            // statesJson.Add("functions", functionsJson);

            // switch (shaderName)
            // {
            //     case "meshbasic":
            //     case "meshlambert":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.None);
            //         this.SetCullFace(enalbesJson, functionsJson, true, FrontFace.CCW, CullFace.BACK);
            //         this.SetDepth(enalbesJson, functionsJson, true, true);
            //         break;
            //     case "meshbasic_doubleside":
            //     case "meshlambert_doubleside":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.None);
            //         this.SetCullFace(enalbesJson, functionsJson, false);
            //         this.SetDepth(enalbesJson, functionsJson, true, true);
            //         break;
            //     case "transparent":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.Blend);
            //         this.SetCullFace(enalbesJson, functionsJson, true, FrontFace.CCW, CullFace.BACK);
            //         this.SetDepth(enalbesJson, functionsJson, true, false);
            //         break;
            //     case "transparent_additive":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.Add);
            //         this.SetCullFace(enalbesJson, functionsJson, true, FrontFace.CCW, CullFace.BACK);
            //         this.SetDepth(enalbesJson, functionsJson, true, false);
            //         break;
            //     case "transparent_doubleside":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.Blend);
            //         this.SetCullFace(enalbesJson, functionsJson, false);
            //         this.SetDepth(enalbesJson, functionsJson, true, false);
            //         break;
            //     case "materialcolor":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.None);
            //         this.SetCullFace(enalbesJson, functionsJson, false);
            //         this.SetDepth(enalbesJson, functionsJson, true, true);
            //         break;
            //     case "particle":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.None);
            //         this.SetCullFace(enalbesJson, functionsJson, false);
            //         this.SetDepth(enalbesJson, functionsJson, true, true);
            //         break;
            //     case "particle_additive":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.Add);
            //         this.SetCullFace(enalbesJson, functionsJson, false);
            //         this.SetDepth(enalbesJson, functionsJson, true, false);
            //         break;
            //     case "particle_blend":
            //         this.SetBlend(enalbesJson, functionsJson, BlendMode.Blend);
            //         this.SetCullFace(enalbesJson, functionsJson, false);
            //         this.SetDepth(enalbesJson, functionsJson, true, false);
            //         break;
            // }

            //
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            materialJson.CovertToStringWithFormat(sb, 4);
            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        private void SetBlend(MyJson_Array enalbesJson, MyJson_Tree functionsJson, BlendMode blend)
        {
            if (blend == BlendMode.None)
            {
                return;
            }
            enalbesJson.AddInt((int)EnableState.BLEND);

            var blendEquationSeparate = new MyJson_Array();
            blendEquationSeparate.AddInt((int)BlendEquation.FUNC_ADD);
            blendEquationSeparate.AddInt((int)BlendEquation.FUNC_ADD);
            var blendFuncSeparate = new MyJson_Array();
            switch (blend)
            {
                case BlendMode.Add:
                    blendFuncSeparate.AddInt((int)BlendFactor.SRC_ALPHA);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.SRC_ALPHA);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    break;
                case BlendMode.Blend:
                    blendFuncSeparate.AddInt((int)BlendFactor.SRC_ALPHA);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE_MINUS_SRC_ALPHA);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE_MINUS_SRC_ALPHA);
                    break;
                case BlendMode.Add_PreMultiply:
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    break;
                case BlendMode.Blend_PreMultiply:
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE_MINUS_CONSTANT_ALPHA);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE);
                    blendFuncSeparate.AddInt((int)BlendFactor.ONE_MINUS_CONSTANT_ALPHA);
                    break;
            }
        }

        private void SetCullFace(MyJson_Array enalbesJson, MyJson_Tree functionsJson, bool cull, FrontFace frontFace = FrontFace.CCW, CullFace cullFace = CullFace.BACK)
        {
            if (cull)
            {
                var frontFaceJson = new MyJson_Array();
                frontFaceJson.AddInt((int)frontFace);
                functionsJson.Add("frontFace", frontFaceJson);

                var cullFaceJson = new MyJson_Array();
                cullFaceJson.AddInt((int)cullFace);
                functionsJson.Add("cullFace", cullFaceJson);

                enalbesJson.AddInt((int)EnableState.CULL_FACE);
            }
        }

        private void SetDepth(MyJson_Array enalbesJson, MyJson_Tree functionsJson, bool zTest, bool zWrite)
        {
            if (zTest)
            {
                var depthFunc = new MyJson_Array();
                depthFunc.AddInt((int)DepthFunc.LEQUAL);
                functionsJson.Add("depthFunc", depthFunc);
                enalbesJson.AddInt((int)EnableState.DEPTH_TEST);
            }

            if (zWrite)
            {
                var depthMask = new MyJson_Array();
                depthMask.AddBool(true);
                functionsJson.Add("depthMask", depthMask);
            }
        }
    }
}
