using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using static iText.Signatures.PdfSigner;

namespace OpenQMS.Services
{
    public static class Helper
    {
        public static string SignPDF(string filePath)
        {
            PdfReader reader = new PdfReader(filePath);
            var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
            string[] pathArr = filePath.Split('\\');
            string fileName = pathArr.Last();
            string signedFile = $"{basePath}Signed_{fileName}";

            FileStream file = new FileStream(signedFile, FileMode.Create, FileAccess.ReadWrite);
            PdfSigner signer = new PdfSigner(reader, file, new StampingProperties());

            string KEYSTORE = Directory.GetCurrentDirectory() + "\\cert.pfx";
            char[] PASSWORD = "asdzxc".ToCharArray();

            Pkcs12Store pk12 = new Pkcs12Store(new FileStream(KEYSTORE, FileMode.Open, FileAccess.Read), PASSWORD);
            string alias = null;
            foreach (var a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                    break;
            }
            ICipherParameters pk = pk12.GetKey(alias).Key;

            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }

            // Create the signature appearance
            iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(200, 200, 150, 70);
            PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
            appearance
                .SetReuseAppearance(false)
                .SetPageRect(rect)
                .SetPageNumber(1)
                .SetLayer2Text("Demo" + System.Environment.NewLine + DateTime.Now.ToShortDateString())
                .SetLayer2FontSize(16);
            signer.SetFieldName("DigitalSignature");

            IExternalSignature pks = new PrivateKeySignature(pk, DigestAlgorithms.SHA256);
            signer.SignDetached(pks, chain, null, null, null, 0, CryptoStandard.CMS);

            return signedFile;
        }
    }
}
