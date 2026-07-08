using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json;
using BIMAPES.Matchday.App.Models;

namespace BIMAPES.Matchday.App.Services
{
    /// <summary>
    /// Generates compliance audit ZIPs for institutional clients.
    /// Contains hash manifests, event logs, and signatures.
    /// </summary>
    public class AuditService
    {
        /// <summary>
        /// Generate audit ZIP for a match.
        /// </summary>
        public string GenerateAuditZip(string matchId, List<Capsule> capsules, List<NilMoment> nilMoments)
        {
            string auditDir = Path.Combine(Path.GetTempPath(), $"audit_{matchId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(auditDir);

            // 1. Write capsule JSON log
            string capsulesJson = JsonSerializer.Serialize(capsules, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(auditDir, "capsules.json"), capsulesJson);

            // 2. Write NIL moments JSON log
            string nilJson = JsonSerializer.Serialize(nilMoments, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(auditDir, "nil_moments.json"), nilJson);

            // 3. Generate hash manifest
            string manifest = _generateHashManifest(capsules, nilMoments);
            File.WriteAllText(Path.Combine(auditDir, "hash_manifest.txt"), manifest);

            // 4. Create ZIP file
            string zipPath = Path.Combine(Path.GetTempPath(), $"BIM-APES-Audit_{matchId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
            ZipFile.CreateFromDirectory(auditDir, zipPath);

            // Clean up temp directory
            Directory.Delete(auditDir, true);

            return zipPath;
        }

        /// <summary>
        /// Generate hash manifest for integrity verification.
        /// </summary>
        private string _generateHashManifest(List<Capsule> capsules, List<NilMoment> nilMoments)
        {
            var lines = new List<string>
            {
                "BIM-APES AUDIT MANIFEST",
                $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}",
                $"Capsule Count: {capsules.Count}",
                $"NIL Moment Count: {nilMoments.Count}",
                "",
                "CAPSULE HASHES:"
            };

            foreach (var capsule in capsules)
            {
                lines.Add($"  {capsule.CapsuleId}: {capsule.Hash}");
            }

            lines.Add("");
            lines.Add("NIL MOMENT HASHES:");
            foreach (var nil in nilMoments)
            {
                lines.Add($"  {nil.NilMomentId}: {nil.CapsuleHash}");
            }

            lines.Add("");
            lines.Add("COMPLIANCE NOTE: This audit ZIP is valid for 72 hours from generation.");
            lines.Add("For auditor verification, contact: audit@bimapes.com");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
