namespace PaperGLTF
{
    using UnityEngine;
    using Egret3DExportTools;
    public class StandardMaterialWriter : BaseMaterialWriter
    {
        protected virtual void StandardBegin()
        {
            var roughness = 1.0f - this.GetFloat("_Glossiness", 0.0f);
            var metalness = this.GetFloat("_Metallic", 0.0f);
            var emissive = this.GetColor("_EmissionColor", Color.black);

            this.values.SetColor3("emissive", emissive);
            this.values.SetNumber("roughness", roughness);
            this.values.SetNumber("metalness", metalness);

            var metalnessMap = this.GetTexture("_MetallicGlossMap", null);
            if (metalnessMap != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(metalnessMap as Texture2D, "");
                this.values.SetString("metalnessMap", texPath);
            }
        }
        protected virtual void StandardEnd()
        {
            this.defines.Add("STANDARD");
        }
        protected override void Update()
        {
            var source = this.source;
            this.StandardBegin();
            var map = this.GetTexture("_MainTex", null);
            if (map != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(map as Texture2D, "");
                this.values.SetString("map", texPath);
            }
            var aoMap = this.GetTexture("_OcclusionMap", null);
            if (aoMap != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(aoMap as Texture2D, "");
                this.values.SetString("aoMap", texPath);
                var aoMapIntensity = this.GetFloat("_OcclusionStrength", 0.0f);
                this.values.SetNumber("aoMapIntensity", aoMapIntensity);
            }

            var emissiveMap = this.GetTexture("_EmissionMap", null);
            if (emissiveMap != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(emissiveMap as Texture2D, "");
                this.values.SetString("emissiveMap", texPath);
            }

            var bumpMap = this.GetTexture("_BumpMap", null);
            if (bumpMap != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(bumpMap as Texture2D, "");
                this.values.SetString("normalMap", texPath);
                var bumpScale = this.GetFloat("_BumpScale", 1.0f);
                this.values.SetVector2("normalScale", new UnityEngine.Vector2(bumpScale, bumpScale));
            }

            var normalMap = this.GetTexture("_DetailNormalMap", null);
            if (normalMap != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(normalMap as Texture2D, "");
                this.values.SetString("normalMap", texPath);
                var normalScale = this.GetFloat("_DetailNormalMapScale", 0.0f);
                this.values.SetVector2("normalScale", new UnityEngine.Vector2(normalScale / 10.0f, normalScale / 10.0f));
            }

            var displacementMap = this.GetTexture("_ParallaxMap", null);
            if (displacementMap != null)
            {
                var texPath = ResourceManager.instance.SaveTexture(displacementMap as Texture2D, "");
                this.values.SetString("displacementMap", texPath);
                var displacementScale = this.GetFloat("_Parallax", 0.0f);
                this.values.SetNumber("displacementScale", displacementScale);
                this.values.SetNumber("displacementBias", 0.0f);
            }

            this.StandardEnd();
        }

        protected override string technique
        {
            get
            {
                return "builtin/meshphysical.shader.json";
            }
        }
    }
}