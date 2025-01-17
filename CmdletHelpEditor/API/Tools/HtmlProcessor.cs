﻿using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text.RegularExpressions;
using CmdletHelpEditor.API.BaseClasses;
using CodeKicker.BBCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace CmdletHelpEditor.API.Tools {
	static class HtmlProcessor {
		static readonly String n = Environment.NewLine;
		public static BBCodeParser GetParser(ParserType type) {
			switch (type) {
				case ParserType.Basic:
					return new BBCodeParser(ErrorMode.ErrorFree, null, new[] {
						new BBTag("b", "<strong>", "</strong>"),
						new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
						new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
						new BBTag("s", "<strike>", "</strike>")
					});
				case ParserType.Enhanced:
					return new BBCodeParser(ErrorMode.ErrorFree, null, new[] {
						new BBTag("br", "<br />", String.Empty),
						new BBTag("b", "<strong>", "</strong>"),
						new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
						new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
						new BBTag("s", "<strike>", "</strike>"),
						new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
						new BBTag("color", "<span style=\"color: ${color};\">","</span>", new BBAttribute("color", ""), new BBAttribute("color", "color"))
					});
				case ParserType.Clear:
					return new BBCodeParser(ErrorMode.ErrorFree, null, new[] {
						new BBTag("br", String.Empty, String.Empty),
						new BBTag("b", String.Empty, String.Empty),
						new BBTag("i", String.Empty, String.Empty),
						new BBTag("u", String.Empty, String.Empty),
						new BBTag("s", String.Empty, String.Empty),
						new BBTag("url", "", "", new BBAttribute("",""),new BBAttribute("","")),
						new BBTag("color", "", "", new BBAttribute("",""),new BBAttribute("",""))
					});
				default:
					return new BBCodeParser(ErrorMode.ErrorFree, null, new[] {
						new BBTag("b", String.Empty, String.Empty),
						new BBTag("i", String.Empty, String.Empty),
						new BBTag("u", String.Empty, String.Empty),
						new BBTag("s", String.Empty, String.Empty)
					});
			}
		}
		
		static String GenerateHtmlLink(String source, IEnumerable<CmdletObject> cmdlets) {
			if (cmdlets != null) {
				foreach (CmdletObject cmdlet in from cmdlet in cmdlets let regex = new Regex(@"\b" + cmdlet.Name + @"\b") where regex.IsMatch(source) == !String.IsNullOrEmpty(cmdlet.URL) select cmdlet) {
					source = source.Replace(cmdlet.Name, "[url=" + cmdlet.URL + "]" + cmdlet.Name + "[/url]");
				}
			}
			return source.Replace("\n", "[br]" + n);
		}
		// generates pure encoded HTML string
		static String GeneratePureHtml(CmdletObject cmdlet, IReadOnlyList<CmdletObject> cmdlets, StringBuilder SB, Boolean useSupports) {
			SB.Clear();
			BBCodeParser rules = GetParser(ParserType.Enhanced);
			HtmlgenerateName(SB, cmdlet);
			HtmlGenerateSynopsis(rules, SB, cmdlets, cmdlet);
			HtmlGenerateSyntax(SB, cmdlet);
			HtmlGenerateDescription(rules, SB, cmdlets, cmdlet);
			HtmlGenerateParams(rules, SB, cmdlets, cmdlet);
			HtmlGenerateInputTypes(rules, SB, cmdlet);
			HtmlGenerateReturnTypes(rules, SB, cmdlet);
			HtmlGenerateNotes(rules, SB, cmdlet);
			HtmlGenerateExamples(rules, SB, cmdlet);
			HtmlGenerateRelatedLinks(rules, SB, cmdlets, cmdlet);
			if (useSupports) { HtmlGenerateSupports(cmdlet, ref SB); }
            if (!String.IsNullOrEmpty(cmdlet.ExtraFooter)) {
                SB.Append(cmdlet.ExtraFooter);
            }
			return SB.ToString();
		}
		static void HtmlgenerateName(StringBuilder SB, CmdletObject cmdlet) {
            if (!String.IsNullOrEmpty(cmdlet.ExtraHeader)) {
                SB.Append(cmdlet.ExtraHeader);
            }
			//SB.Append("<h2>NAME</h2>" + n);
			SB.Append("<h1 style=\"text-align: center;\"><strong>" + SecurityElement.Escape(cmdlet.Name) + "</strong></h1>" + n);
		}
		static void HtmlGenerateSynopsis(BBCodeParser rules, StringBuilder SB, IReadOnlyList<CmdletObject> cmdlets, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Synopsis</strong></h2>" + n);
			if (!String.IsNullOrEmpty(cmdlet.GeneralHelp.Synopsis)) {
				String str = rules.ToHtml(GenerateHtmlLink(cmdlet.GeneralHelp.Synopsis, cmdlets));
				SB.Append("<p style=\"margin-left: 40px;\">" + str + "</p>" + n);
			}
		}
		static void HtmlGenerateSyntax(StringBuilder SB, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Syntax</strong></h2>" + n);
			SB.Append("<pre style=\"margin-left: 40px;\">");
			foreach (String syntaxItem in cmdlet.Syntax) {
				SB.Append(SecurityElement.Escape(syntaxItem) + " [&lt;CommonParameters&gt;]" + n + n);
			}
			SB.Append("</pre>" + n);
		}
		static void HtmlGenerateDescription(BBCodeParser rules, StringBuilder SB, IReadOnlyList<CmdletObject> cmdlets, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Description</strong></h2>" + n);
			if (!String.IsNullOrEmpty(cmdlet.GeneralHelp.Description)) {
				String str = rules.ToHtml(GenerateHtmlLink(cmdlet.GeneralHelp.Description, cmdlets));
				SB.Append("<p style=\"margin-left: 40px;\">" + str + "</p>" + n);
			}
		}
		static void HtmlGenerateParams(BBCodeParser rules, StringBuilder SB, IReadOnlyList<CmdletObject> cmdlets, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Parameters</strong></h2>" + n);
			foreach (ParameterDescription param in cmdlet.Parameters) {
				SB.Append("<h3><strong>-" + SecurityElement.Escape(param.Name) + "</strong> <em style=\"font-weight: 100;\">&lt;" + SecurityElement.Escape(param.Type) + "&gt;</em></h3>" + n);
				if (!String.IsNullOrEmpty(param.Description)) {
					String str = rules.ToHtml(GenerateHtmlLink(param.Description, cmdlets));
					SB.Append("<p style=\"margin-left: 40px; text-align: left;\">" + str + "</p>" + n);
				}
				SB.Append("<table border=\"1\" style=\"margin-left: 40px;\">" + n);
				SB.Append("	<tbody>" + n);
				SB.Append("		<tr>" + n);
				SB.Append("			<td>Required?</td>" + n);
				SB.Append("			<td>" + Convert.ToString(param.Mandatory) + "</td>" + n);
				SB.Append("		</tr>" + n);
				SB.Append("		<tr>" + n);
				SB.Append("			<td>Position?</td>" + n);
				SB.Append("			<td>" + param.Position + "</td>" + n);
				SB.Append("		</tr>" + n);
				SB.Append("		<tr>" + n);
				SB.Append("			<td>Default value</td>" + n);
				if (String.IsNullOrEmpty(param.DefaultValue)) {
					SB.Append("			<td>&nbsp;</td>" + n);
				} else {
					SB.Append("			<td>" + param.DefaultValue + "</td>" + n);
				}
				SB.Append("		</tr>" + n);
				SB.Append("		<tr>" + n);
				SB.Append("			<td>Accept pipeline input?</td>" + n);
				if (param.Pipeline || param.PipelinePropertyName) {
					SB.Append("			<td>true");
					if (param.Pipeline && !param.PipelinePropertyName) {
						SB.Append(" (ByValue)");
					} else if (param.Pipeline && param.PipelinePropertyName) {
						SB.Append(" (ByValue, ByPropertyName)");
					} else {
						SB.Append(" (ByPropertyName)");
					}
					SB.Append("</td>" + n);
				} else {
					SB.Append("			<td>false</td>" + n);
				}
				SB.Append("		</tr>" + n);
				SB.Append("		<tr>" + n);
				SB.Append("			<td>Accept wildcard characters?</td>" + n);
				SB.Append("			<td>" + Convert.ToString(param.Globbing) + "</td>" + n);
				SB.Append("		</tr>" + n);
				SB.Append("	</tbody>" + n);
				SB.Append("</table>" + n);
			}
			// Common parameters
			SB.Append("<h3>&lt;CommonParameters&gt;</h3>" + n);
			SB.Append("<p style=\"margin-left: 40px;\">This cmdlet supports the common parameters: Verbose, Debug,<br />" + n);
			SB.Append("ErrorAction, ErrorVariable, InformationAction, InformationVariable,<br />" + n);
			SB.Append("WarningAction, WarningVariable, OutBuffer, PipelineVariable and OutVariable.<br />" + n);
			SB.Append("For more information, see about_CommonParameters (<a href=\"http://go.microsoft.com/fwlink/?LinkID=113216\">http://go.microsoft.com/fwlink/?LinkID=113216</a>).</p>" + n);
		}
		static void HtmlGenerateInputTypes(BBCodeParser rules, StringBuilder SB, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Inputs</strong></h2>" + n);
			List<String> inputTypes = new List<String>(cmdlet.GeneralHelp.InputType.Split(new[] { ';' }));
			List<String> inputUrls = new List<String>(cmdlet.GeneralHelp.InputUrl.Split(new[] { ';' }));
			List<String> inputDescription = new List<String>(cmdlet.GeneralHelp.InputTypeDescription.Split(new[] { ';' }));
			for (Int32 index = 0; index < inputTypes.Count; index++) {
				if (index < inputUrls.Count) {
					if (String.IsNullOrEmpty(inputUrls[index])) {
						SB.Append("<p style=\"margin-left: 40px;\">" + rules.ToHtml(inputTypes[index]) + "</p>" + n);
					} else {
						SB.Append("<p style=\"margin-left: 40px;\"><a href=\"" + inputUrls[index] + "\">" + rules.ToHtml(inputTypes[index]) + "</a></p>" + n);
					}
				} else {
					SB.Append("<p style=\"margin-left: 40px;\">" + rules.ToHtml(inputTypes[index]) + "</p>" + n);
				}
				if (index < inputDescription.Count) {
					SB.Append("<p style=\"margin-left: 80px;\">" + rules.ToHtml(inputDescription[index]) + "</p>" + n);
				}
			}
		}
		static void HtmlGenerateReturnTypes(BBCodeParser rules, StringBuilder SB, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Outputs</strong></h2>" + n);
			List<String> returnTypes = new List<String>(cmdlet.GeneralHelp.ReturnType.Split(new[] { ';' }));
			List<String> returnUrls = new List<String>(cmdlet.GeneralHelp.ReturnUrl.Split(new[] { ';' }));
			List<String> returnDescription = new List<String>(cmdlet.GeneralHelp.ReturnTypeDescription.Split(new[] { ';' }));
			for (Int32 index = 0; index < returnTypes.Count; index++) {
				if (index < returnUrls.Count) {
					if (String.IsNullOrEmpty(returnUrls[index])) {
						SB.Append("<p style=\"margin-left: 40px;\">" + rules.ToHtml(returnTypes[index]) + "</p>" + n);
					} else {
						SB.Append("<p style=\"margin-left: 40px;\"><a href=\"" + returnUrls[index] + "\">" + rules.ToHtml(returnTypes[index]) + "</a></p>" + n);
					}
				} else {
					SB.Append("<p style=\"margin-left: 40px;\">" + rules.ToHtml(returnTypes[index]) + "</p>" + n);
				}
				if (index < returnDescription.Count) {
					SB.Append("<p style=\"margin-left: 80px;\">" + rules.ToHtml(returnDescription[index]) + "</p>" + n);
				}
			}
		}
		static void HtmlGenerateNotes(BBCodeParser rules, StringBuilder SB, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Notes</strong></h2>" + n);
			if (!String.IsNullOrEmpty(cmdlet.GeneralHelp.Notes)) {
				String str = rules.ToHtml(GenerateHtmlLink(cmdlet.GeneralHelp.Notes, null));
				SB.Append("<p style=\"margin-left: 40px;\">" + str + "</p>" + n);
			}
		}
		static void HtmlGenerateExamples(BBCodeParser rules, StringBuilder SB, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Examples</strong></h2>" + n);
			foreach (Example example in cmdlet.Examples) {
				String name = String.IsNullOrEmpty(example.Name) ? "unknown" : example.Name;
				SB.Append("<h3>" + SecurityElement.Escape(name) + "</h3>" + n);
				if (!String.IsNullOrEmpty(example.Cmd)) {
					String cmd;
					if (!example.Cmd.StartsWith("PS C:\\>")) { cmd = "PS C:\\> " + example.Cmd; } else { cmd = example.Cmd; }
					SB.Append("<pre style=\"margin-left: 40px;\">" + SecurityElement.Escape(cmd) + "</pre>" + n);
				}
				if (!String.IsNullOrEmpty(example.Output)) {
					SB.Append("<pre style=\"margin-left: 40px;\">" + SecurityElement.Escape(example.Output) + "</pre>" + n);
				}
				if (!String.IsNullOrEmpty(example.Description)) {
					String str = rules.ToHtml(example.Description);
					SB.Append("<p style=\"margin-left: 40px;\">" + str + "</p>" + n);
				}
			}
		}
		static void HtmlGenerateRelatedLinks(BBCodeParser rules, StringBuilder SB, IReadOnlyList<CmdletObject> cmdlets, CmdletObject cmdlet) {
			SB.Append("<h2><strong>Related links</strong></h2>" + n);
			if (cmdlet.RelatedLinks.Count > 0) {
				SB.Append("<p style=\"margin-left: 40px;\">" + n);
				foreach (RelatedLink link in cmdlet.RelatedLinks.Where(x => x.LinkText.ToLower() != "online version:")) {
					SB.Append("	" + rules.ToHtml(GenerateHtmlLink(link.LinkText, cmdlets)));
					if (!String.IsNullOrEmpty(link.LinkUrl)) {
						SB.Append("	<a href=\"" + link.LinkUrl + "\">" + link.LinkUrl + "</a>");
					}
					SB.Append("<br />" + n);
				}
				SB.Append("</p>");
			}
		}
		static void HtmlGenerateSupports(CmdletObject cmdlet, ref StringBuilder SB) {
			String currentHtml = String.Empty;
			if (cmdlet.SupportInformation.ADChecked) {
				currentHtml += "<p style=\"color: red; text-align: center;\">[This command is not available in non-domain environments]</p>" + n;
			} if (cmdlet.SupportInformation.RsatChecked) {
				currentHtml += "<p style=\"color: red; text-align: center;\">[This command requires installed Remote Server Administration Tools (RSAT)]</p>" + n;
			}
			SB = new StringBuilder(currentHtml + SB);
			SB.Append("<h2><strong>PowerShell Support</strong></h2>" + n);
			String psver;
			if (cmdlet.SupportInformation.Ps2Checked) {
				psver = "PowerShell 2.0";
			} else if (cmdlet.SupportInformation.Ps3Checked) {
				psver = "PowerShell 3.0";
			} else if (cmdlet.SupportInformation.Ps4Checked) {
				psver = "PowerShell 4.0";
            } else if (cmdlet.SupportInformation.Ps5Checked) {
                psver = "PowerShell 5.0";
            } else {
				psver = "Any";
			}
			SB.Append("<ul>" + n + "	<li>" + psver + "</li>" + n + "</ul>");
			SB.Append("<h2><strong>Operating System Support</strong></h2>" + n + "<ul>" + n);
			if (cmdlet.SupportInformation.WinXpChecked) { SB.Append("	<li>Windows XP</li>" + n); }
			if (cmdlet.SupportInformation.WinVistaChecked) { SB.Append("	<li>Windows Vista</li>" + n); }
			if (cmdlet.SupportInformation.Win7Checked) { SB.Append("	<li>Windows 7</li>" + n); }
			if (cmdlet.SupportInformation.Win8Checked) { SB.Append("	<li>Windows 8</li>" + n); }
			if (cmdlet.SupportInformation.Win81Checked) { SB.Append("	<li>Windows 8.1</li>" + n); }
            if (cmdlet.SupportInformation.Win10Checked) { SB.Append("	<li>Windows 10</li>" + n); }
            if (cmdlet.SupportInformation.Win2003Checked) {
				SB.Append("	<li>Windows Server 2003 all editions</li>" + n);
			} else {
				if (cmdlet.SupportInformation.Win2003StdChecked) { SB.Append("	<li>Windows Server 2003 Standard</li>" + n); }
				if (cmdlet.SupportInformation.Win2003EEChecked) { SB.Append("	<li>Windows Server 2003 Enterprise</li>" + n); }
				if (cmdlet.SupportInformation.Win2003DCChecked) { SB.Append("	<li>Windows Server 2003 Datacenter</li>" + n); }
			}
			if (cmdlet.SupportInformation.Win2008Checked) {
				SB.Append("	<li>Windows Server 2008 all editions</li>" + n);
			} else {
				if (cmdlet.SupportInformation.Win2008StdChecked) { SB.Append("	<li>Windows Server 2008 Standard</li>" + n); }
				if (cmdlet.SupportInformation.Win2008EEChecked) { SB.Append("	<li>Windows Server 2008 Enterprise</li>" + n); }
				if (cmdlet.SupportInformation.Win2008DCChecked) { SB.Append("	<li>Windows Server 2008 Datacenter</li>" + n); }
			}
			if (cmdlet.SupportInformation.Win2008R2Checked) {
				SB.Append("	<li>Windows Server 2008 R2 all editions</li>" + n);
			} else {
				if (cmdlet.SupportInformation.Win2008R2StdChecked) { SB.Append("	<li>Windows Server 2008 R2 Standard</li>" + n); }
				if (cmdlet.SupportInformation.Win2008R2EEChecked) { SB.Append("	<li>Windows Server 2008 R2 Enterprise</li>" + n); }
				if (cmdlet.SupportInformation.Win2008R2DCChecked) { SB.Append("	<li>Windows Server 2008 R2 Datacenter</li>" + n); }
			}
			if (cmdlet.SupportInformation.Win2012Checked) {
				SB.Append("	<li>Windows Server 2012 all editions</li>" + n);
			} else {
				if (cmdlet.SupportInformation.Win2012StdChecked) { SB.Append("	<li>Windows Server 2012 Standard</li>" + n); }
				if (cmdlet.SupportInformation.Win2012DCChecked) { SB.Append("	<li>Windows Server 2012 Datacenter</li>" + n); }
			}
			if (cmdlet.SupportInformation.Win2012R2Checked) {
				SB.Append("	<li>Windows Server 2012 R2 all editions</li>" + n);
			} else {
				if (cmdlet.SupportInformation.Win2012R2StdChecked) { SB.Append("	<li>Windows Server 2012 R2 Standard</li>" + n); }
				if (cmdlet.SupportInformation.Win2012R2DCChecked) { SB.Append("	<li>Windows Server 2012 R2 Datacenter</li>" + n); }
			}
			SB.Append("</ul>");
		}

		// generates HTML for web browser control
		public static Task<String> GenerateHtmlView(CmdletObject cmdlet, ModuleObject moduleObject) {
			return Task<String>.Factory.StartNew(() => GeneratePureHtml(cmdlet, moduleObject.Cmdlets, new StringBuilder(), moduleObject.UseSupports));
		}
		// generates HTML for HTML source view
		public static Task<IEnumerable<XmlToken>> GenerateHtmlSourceHelp(CmdletObject cmdlet, ModuleObject moduleObject) {
			return Task<IEnumerable<XmlToken>>.Factory.StartNew(() =>
				XmlTokenizer.LoopTokenize(
					GeneratePureHtml(cmdlet, moduleObject.Cmdlets, new StringBuilder(), moduleObject.UseSupports)
				)
			);
		}
	}
}
