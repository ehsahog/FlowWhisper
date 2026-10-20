namespace FlowWhisper.Core.Assets;

/// <param name="RelativePath">Path under the asset's install directory.</param>
/// <param name="Sha256">Lower-case hex SHA-256; the download is rejected if it doesn't match.</param>
public sealed record RemoteFile(string RelativePath, string Url, long Size, string Sha256);

/// <summary>
/// Everything FlowWhisper downloads on first use. URLs are pinned to exact
/// revisions and every file is hash-checked, so a changed or tampered upstream
/// file is rejected instead of silently installed.
/// </summary>
public static class AssetManifest
{
    /// <summary>Directory name under models/ plus the files that make up one OpenVINO Whisper export.</summary>
    public sealed record WhisperModel(string Directory, IReadOnlyList<RemoteFile> Files);

    public const string DefaultWhisperModel = "turbo";

    // Intel's OpenVINO exports of multilingual Whisper (the ".en"/distil variants can't do
    // Swedish). Hashes computed from verified downloads; LFS files also match Hugging Face's.
    private static readonly Dictionary<string, WhisperModel> WhisperModels = new()
    {
        ["small"] = OvWhisper("whisper-small-int8-ov", "5b831719e093f86e1970be663e524fe001489f9b",
        [
            ("added_tokens.json", 34_604, "9715fd2243b6f06a5858b5e32950d2853f73dd5bc201aafcf76f5082a2d8acd1"),
            ("config.json", 1_325, "2d1cf9f56daeb3c1afabb7fbd9df05e9d9e00d4aca728be033097cd9ab8ebf1f"),
            ("generation_config.json", 3_841, "bf619a40ae65e028b089124d04b294b0e8aabfbc0f9f8c5b93b584652d15ec5e"),
            ("merges.txt", 493_869, "2df2990a395e35e8dfbc7511e08c12d56018d8d04691e0133e5d63b21e154dc6"),
            ("normalizer.json", 52_666, "bf1c507dc8724ca9cf9903640dacfb69dae2f00edee4f21ceba106a7392f26dd"),
            ("openvino_config.json", 445, "dce6b43e54dab3eb21d6995fd113dc49f337fd2e650e3c63ed849bcf7bfbd54d"),
            ("openvino_decoder_model.bin", 154_571_955, "836642928d3354f0f239a45a4166bdd42647cc0e085b7e9d85b303d050e55a3b"),
            ("openvino_decoder_model.xml", 1_092_851, "d44d10b6ff3ce686f1d24cc932c3a178dde693fc1a4107474ce79aaafe6d33bd"),
            ("openvino_detokenizer.bin", 736_181, "2542d1fe6c4c5d838e7b7c61b24996b94ae0cb54e64ff6dbaab2b9dd5ddd7ca0"),
            ("openvino_detokenizer.xml", 9_699, "2a3166e38653bb5531a44bab187ffabefdb49f70069ab3d7a05f2b7dd816dfa0"),
            ("openvino_encoder_model.bin", 92_204_656, "cb4ce5067a24d0edf1f29ce4ed7162958be9c5efa7f4cc357b23aaac224d736e"),
            ("openvino_encoder_model.xml", 574_964, "778225a033690a0661cfdfd79352c569e34943754ef394234df8ffb507049430"),
            ("openvino_tokenizer.bin", 1_898_933, "846f3c65f7a71f120fce7aaaf41b342f0767eb13e3c4e7c2a54f1d49b5c38fda"),
            ("openvino_tokenizer.xml", 27_011, "9b362d31bae5855e5c4f5e2e1a2196337264b84f546161333027e34ed92a44dd"),
            ("preprocessor_config.json", 356, "994838f1fa6462c8b9b3c90edada831f11f3dd8b4664634e18f4694d005c9dbf"),
            ("special_tokens_map.json", 2_194, "e67ae3a0aaa99abcd9f187138e12db1f65c16a14761c50ef10eef2c174a7a691"),
            ("tokenizer.json", 3_930_494, "7b469ff15eb7816315aa45eec391f5943d639b9d73d110f5c003df5192fd54e3"),
            ("tokenizer_config.json", 282_713, "21a4fc0483c14b87f4e0bbc177a9a357479bfa7c95aaf21ad53d71e9c5afafb9"),
            ("vocab.json", 835_550, "8f680bba319e01a653d2e8a5dbc17a9157179e0576e6ce74ce0c06356c6e24f9"),
        ]),
        ["turbo"] = OvWhisper("whisper-large-v3-turbo-int4-ov", "ae50b4d9a9dbaf16f2df59c23f3984e42f864dfc",
        [
            ("added_tokens.json", 34_648, "3c51f66c4c21f9e126970078f11ae77a78c74aee8df606ee9daba86e467108e0"),
            ("config.json", 1_192, "bfd92c097547ab12cb42abae8008be5a59a91fdc5ab39acce24489eb8a3e8a86"),
            ("generation_config.json", 3_767, "4617fcca458af3b91a103143aaac919c1ab6680b552d7abd10811b7248bd77b4"),
            ("merges.txt", 493_869, "2df2990a395e35e8dfbc7511e08c12d56018d8d04691e0133e5d63b21e154dc6"),
            ("normalizer.json", 52_666, "bf1c507dc8724ca9cf9903640dacfb69dae2f00edee4f21ceba106a7392f26dd"),
            ("openvino_config.json", 622, "9da38e88cec069ad54699d627f1c59d36c36a3dfb54b5e1bb5cfdf5832efaf03"),
            ("openvino_decoder_model.bin", 121_954_278, "70a34bd26aa8b802535420239d61b8d6cc0876ef777046d395c788435bc5572e"),
            ("openvino_decoder_model.xml", 433_734, "8306639b2f235c3a2b9a8a7821c21fbdd096e0b1c107868b1dd29e57f2e2588c"),
            ("openvino_detokenizer.bin", 736_198, "f2b3c47825a1089525ff65c0c8e49271e1dee69a401a04fc827ac2de5b7766e4"),
            ("openvino_detokenizer.xml", 9_779, "6e106a14f14b0771b46b7948a99b1d819ff93b2455b7da8f47761ab9dba9dc56"),
            ("openvino_encoder_model.bin", 345_094_560, "86edfce9765b229a6a47b0036ef2eb96ff898675d424c51fc2b165e088d3bbc4"),
            ("openvino_encoder_model.xml", 1_721_370, "ac2adae65fcc6f6ea958e3a180570e7eca3a453f448387cc703080db6ab88880"),
            ("openvino_tokenizer.bin", 1_898_973, "adfa3d9a2920d0f314121270a960ab331ec0f05838544bb8ecaaa422282a6fd4"),
            ("openvino_tokenizer.xml", 27_091, "cba304e7bad54773b9d2cbccfbc8501117ecf2e3c0f4f5331742a0a3c9feed93"),
            ("preprocessor_config.json", 357, "654cf18d3e163b948ceaf9766da56ce0b52de265d58673cf61c9376f126bd499"),
            ("special_tokens_map.json", 2_186, "baea4ea09372eb4fca86b4e4346139fd73cb807d5087e9de0948e971739c3e74"),
            ("tokenizer.json", 3_930_645, "5c1bf30c9e716e1477bedef846b01be0013daecb89e9e3ef7ab89b23c178df1b"),
            ("tokenizer_config.json", 282_873, "3c75940dfce3a294fca7041a5faff011677f1b68fa85e47511bb8cf6dccaded6"),
            ("vocab.json", 835_528, "6788c80b082e9b0d1393147d3a3e62ba19285ac0c82ace8e5ef00f37ead58971"),
        ]),
    };

    public static bool IsKnownWhisperModel(string model) => WhisperModels.ContainsKey(model);

    public static WhisperModel GetWhisperModel(string model) =>
        WhisperModels.TryGetValue(model, out var m)
            ? m
            : throw new ArgumentException($"Unknown Whisper model '{model}'.", nameof(model));

    private static WhisperModel OvWhisper(string repo, string revision, (string Name, long Size, string Sha256)[] files) =>
        new("ov-" + repo, files
            .Select(f => new RemoteFile(f.Name, $"https://huggingface.co/OpenVINO/{repo}/resolve/{revision}/{f.Name}", f.Size, f.Sha256))
            .ToList());

    private const string CleanupRevision = "dc27b4e451118b1e82c159a839476df02797837a";
    private const string CleanupBase =
        $"https://huggingface.co/OpenVINO/Phi-3.5-mini-instruct-int4-cw-ov/resolve/{CleanupRevision}/";

    /// <summary>
    /// The subset of Intel's Phi-3.5-mini OpenVINO export that GenAI actually needs —
    /// verified to load and generate without the repo's Python code and HF tokenizer files.
    /// </summary>
    public static readonly IReadOnlyList<RemoteFile> CleanupModel =
    [
        CleanupFile("added_tokens.json", 293, "5f5c94d30901fe9c961d776d51eadbda4454991a77b53d36e0be0571a8e72a7d"),
        CleanupFile("chat_template.jinja", 430, "78d976a442bcde2f0be15aafbb8e3050e1104f86732266f68403251b89982a90"),
        CleanupFile("config.json", 3_406, "ccfd2129c49ba301b921b6274a663ad54f083b98fc387ec90d4340ff863e8eae"),
        CleanupFile("generation_config.json", 172, "42fdb2f9dfc6b51fb1dfebb7504c4f1d347a195e4ae26a5abeaeb2b45b335511"),
        CleanupFile("openvino_config.json", 528, "b64e1fadc14524cd8e2332f165bbbbf1c87ea8858f6552ea5a90f13a68f17534"),
        CleanupFile("openvino_detokenizer.bin", 467_213, "b16873376036dd344f0ca3b4be43fd13243a0ab708eafa7560a301531093586f"),
        CleanupFile("openvino_detokenizer.xml", 11_436, "d60d25ae8002525967e6a5d6238d0d5977cf438cdd0042093c898a926ed9aa1f"),
        CleanupFile("openvino_model.bin", 2_011_963_706, "9ad3670e9af5f4fddb70e8394882aba466f6e012ad3c58d9fce2ade7d86625fd"),
        CleanupFile("openvino_model.xml", 2_372_534, "b8ddce888b9dcdc3d3e75cf1d367b6867573ae887c41074625eae94339f43811"),
        CleanupFile("openvino_tokenizer.bin", 1_869_678, "6b0b3d91de670d3003843820874428c851b19f5e15eeaa7f78c8b95eb7fd15fe"),
        CleanupFile("openvino_tokenizer.xml", 25_208, "f6c7bf750929fe6bbd9e73f7c853e0a2a38a247f4e0bd09cb9cb30097cb59593"),
        CleanupFile("special_tokens_map.json", 569, "474d699677e264edd3851559548cbbb61801ad8da6b249c81455007602313c34"),
        CleanupFile("tokenizer_config.json", 2_934, "78f1699f672ba5c8c958fa9f69040560a0310967db88fcba80746d299738f13c"),
    ];

    /// <summary>Intel's official OpenVINO GenAI runtime; contains openvino_genai_c.dll.</summary>
    public static readonly RemoteFile OpenVinoRuntimeArchive = new(
        "openvino_genai_windows_2026.4.1.0_x86_64.zip",
        "https://storage.openvinotoolkit.org/repositories/openvino_genai/packages/2026.4.1/windows/openvino_genai_windows_2026.4.1.0_x86_64.zip",
        254_143_615,
        "437dc85e0fa60d2231b28f323c9b06ee27bc5eb4d3285fb266f9dd5fb012d0b8");

    public const string OpenVinoRuntimeVersion = "2026.4.1";

    private static RemoteFile CleanupFile(string name, long size, string sha256) =>
        new(name, CleanupBase + name, size, sha256);
}
