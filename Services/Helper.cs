/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using static iText.Signatures.PdfSigner;

namespace OpenQMS.Services
{
    public static class Helper
    {
        public static /*string*/byte[] SignPDF(/*string filePath*/byte[] fileContent)
        {
            MemoryStream pdfStream = new MemoryStream(fileContent);
            PdfReader reader = new PdfReader(/*filePath*/pdfStream);
            //var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
            //string[] pathArr = filePath.Split('\\');
            //string fileName = pathArr.Last();
            //string signedFile = $"{basePath}Signed_{fileName}";

            //FileStream file = new FileStream(signedFile, FileMode.Create, FileAccess.ReadWrite);
            MemoryStream outputStream = new MemoryStream();
            PdfSigner signer = new PdfSigner(reader, /*file*/outputStream, new StampingProperties());

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

            byte[] signedFile = outputStream.ToArray();
            return signedFile;
        }
    }
}
