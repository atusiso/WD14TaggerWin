using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElBruno.HuggingFace;

namespace WD14TaggerWin.ModelManager
{
    /// <summary>
    /// 対象のモデル管理
    /// </summary>
    public class ModelMan
    {
        /// <summary>
        /// サポートInterrogatorリスト
        /// </summary>
        public Dictionary<string, AbstractTaggerModel> interrogators = new Dictionary<string, AbstractTaggerModel>()
        {
             { "wd14-vit.v1", new WaifuDiffusionTaggerModel
                { name="WD14 ViT v1", repo_id="SmilingWolf/wd-v1-4-vit-tagger", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-vit-tagger/"}}
            ,{ "wd14-vit.v2", new WaifuDiffusionTaggerModel
                { name="WD14 ViT v2", repo_id="SmilingWolf/wd-v1-4-vit-tagger-v2", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-vit-tagger-v2/"}}
            ,{ "wd14-convnext.v1", new WaifuDiffusionTaggerModel
                { name="WD14 ConvNeXT v1", repo_id="SmilingWolf/wd-v1-4-convnext-tagger", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-convnext-tagger/"}}
            ,{ "wd14-convnext.v2", new WaifuDiffusionTaggerModel
                { name="WD14 ConvNeXT v2", repo_id="SmilingWolf/wd-v1-4-convnext-tagger-v2", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-convnext-tagger-v2/"}}
            ,{ "wd14-convnextv2.v1", new WaifuDiffusionTaggerModel
                { name="WD14 ConvNeXTV2 v1", repo_id="SmilingWolf/wd-v1-4-convnextv2-tagger-v2", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-convnextv2-tagger-v2/"}}
            ,{ "wd14-swinv2-v1", new WaifuDiffusionTaggerModel
                { name="WD14 SwinV2 v1", repo_id="SmilingWolf/wd-v1-4-swinv2-tagger-v2", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-swinv2-tagger-v2/"}}
            ,{ "wd-v1-4-moat-tagger.v2", new WaifuDiffusionTaggerModel
                { name="WD14 moat tagger v2", repo_id="SmilingWolf/wd-v1-4-moat-tagger-v2", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-v1-4-moat-tagger-v2/"}}
            ,{ "wd-v1-4-vit-tagger.v3", new WaifuDiffusionTaggerModel
                { name="WD14 ViT v3", repo_id="SmilingWolf/wd-vit-tagger-v3", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-vit-tagger-v3/"}}
            ,{ "wd-v1-4-convnext-tagger.v3", new WaifuDiffusionTaggerModel
                { name="WD14 ConvNext v3", repo_id="SmilingWolf/wd-convnext-tagger-v3", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-convnext-tagger-v3/"}}
            ,{ "wd-v1-4-swinv2-tagger.v3", new WaifuDiffusionTaggerModel
                { name="WD14 SwinV2 v3", repo_id="SmilingWolf/wd-swinv2-tagger-v3", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-swinv2-tagger-v3/"}}
            ,{ "wd-vit-large-tagger-v3", new WaifuDiffusionTaggerModel
                { name="WD ViT-Large Tagger v3", repo_id="SmilingWolf/wd-vit-large-tagger-v3", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-vit-large-tagger-v3/"}}
            ,{ "wd-eva02-large-tagger-v3", new WaifuDiffusionTaggerModel
                { name="WD EVA02-Large Tagger v3", repo_id="SmilingWolf/wd-eva02-large-tagger-v3", model_path= "model.onnx", tag_path = "selected_tags.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="SmilingWolf", siteuri="https://huggingface.co/SmilingWolf/wd-eva02-large-tagger-v3/"}}
            ,{ "z3d-e621-convnext-toynya", new WaifuDiffusionTaggerModel
                { name="Z3D-E621-Convnext(toynya)", repo_id="toynya/Z3D-E621-Convnext", model_path= "model.onnx", tag_path = "tags-selected.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="toynya", siteuri="https://huggingface.co/toynya/Z3D-E621-Convnext/"}}
            ,{ "z3d-e621-convnext-silveroxides", new WaifuDiffusionTaggerModel
                { name="Z3D-E621-Convnext(silveroxides)", repo_id="silveroxides/Z3D-E621-Convnext", model_path= "model.onnx", tag_path = "tags-selected.csv", license = AbstractTaggerModel.licenseType.Apache2_0, author="silveroxides", siteuri="https://huggingface.co/silveroxides/Z3D-E621-Convnext/"}}
            ,{ "mld-caformer.dec-5-97527", new MLDanbooruTaggerModel
                { name="ML-Danbooru Caformer dec-5-97527", repo_id="deepghs/ml-danbooru-onnx", model_path= "ml_caformer_m36_dec-5-97527.onnx", tag_path = "classes.json", license = AbstractTaggerModel.licenseType.MIT, author="deepghs", siteuri="https://huggingface.co/deepghs/ml-danbooru-onnx/"}}
            ,{ "mld-tresnetd.6-30000", new MLDanbooruTaggerModel
                { name="ML-Danbooru TResNet-D 6-30000", repo_id="deepghs/ml-danbooru-onnx", model_path= "TResnet-D-FLq_ema_6-30000.onnx", tag_path = "classes.json", license = AbstractTaggerModel.licenseType.MIT, author="deepghs", siteuri="https://huggingface.co/deepghs/ml-danbooru-onnx/"}}
            ,{ "camie-tagger", new CamieTaggerModel
                { name="Camie Tagger", repo_id="Camais03/camie-tagger", model_path= "model_initial.onnx", tag_path = "model_initial_metadata.json", license = AbstractTaggerModel.licenseType.GPL3_0, author="Camais03", siteuri="https://huggingface.co/Camais03/camie-tagger/"}}
            ,{ "camie-tagger-v2", new CamieTaggerModel
                { name="Camie Tagger v2", repo_id="Camais03/camie-tagger-v2", model_path= "camie-tagger-v2.onnx", tag_path = "camie-tagger-v2-metadata.json", license = AbstractTaggerModel.licenseType.GPL3_0, author="Camais03", siteuri="https://huggingface.co/Camais03/camie-tagger-v2/"}}
            ,{ "cl-tagger-1.02", new ClTaggerModel
                { name="Cl Tagger 1.02", repo_id="cella110n/cl_tagger", model_path= "cl_tagger_1_02/model.onnx", tag_path = "cl_tagger_1_02/tag_mapping.json", license = AbstractTaggerModel.licenseType.Apache2_0, author="cella110n", siteuri="https://huggingface.co/cella110n/cl_tagger/"}}
            ,{ "cl-tagger-1.02_opt", new ClTaggerModel
                { name="Cl Tagger 1.02 optimized", repo_id="cella110n/cl_tagger", model_path= "cl_tagger_1_02/model_optimized.onnx", tag_path = "cl_tagger_1_02/tag_mapping.json", license = AbstractTaggerModel.licenseType.Apache2_0, author="cella110n", siteuri="https://huggingface.co/cella110n/cl_tagger/"}}
        };

        /// <summary>キャッシュパス</summary>
        private string CachePath = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ModelMan()
        {
        }

        /// <summary>
        /// インデクサ
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>モデル実体</returns>
        public AbstractTaggerModel? this[string key]
        {
            get 
            {
                if (interrogators.ContainsKey(key) == false) return null;
                return interrogators[key];
            }
        }

        /// <summary>
        /// キャッシュパスの設定
        /// </summary>
        /// <param name="cachePath">キャッシュパス</param>
        public void ChangeCachePath(string cachePath)
        {
            // キャッシュパスの変更
            CachePath = cachePath;
        }

        /// <summary>
        /// モデル一覧の取得
        /// </summary>
        /// <returns>モデル一覧</returns>
        public List<(string, string)> Models()
        {
            List<(string, string)> res = new List<(string, string)>();

            foreach (var interrogator in interrogators)
            {
                res.Add((interrogator.Key, interrogator.Value.name));
            }

            return res;
        }

        /// <summary>
        /// メモリ上のモデルの解放
        /// </summary>
        public void FreeAllModels()
        {
            foreach (var interrogator in interrogators)
            {
                interrogator.Value.UnloadModel();
            }
        }

    }
}
