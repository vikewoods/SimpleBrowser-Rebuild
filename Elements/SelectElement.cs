﻿using System.Diagnostics;

namespace SimpleBrowser.Elements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal class SelectElement : FormElementElement
	{
		private IEnumerable<OptionElement> _options = null;

		public SelectElement(XElement element)
			: base(element)
		{
		}

		public override string Value
		{
			get
			{
				var options = this.Options;
				var optionElement = options.Where(d => d.Selected).FirstOrDefault() ?? options.FirstOrDefault();
				if (optionElement == null)
				{
				    return null;
				}

				var valueAttr = optionElement.OptionValue;
				return valueAttr;
			}
			set
			{
                    //todo: use Options and OptionValue
                    foreach (XElement x in Element.Descendants("option"))
                    {
                        var attr = GetAttribute(x, "value");
                        string val = attr?.Value.Trim() ?? x.Value.Trim();
                        x.SetAttributeValue("selected", val == value.Trim() ? "selected" : null);
                    }
            }
		}

        public bool MultiValued
		{
			get
			{
				return (Element.GetAttribute("multiple") != null);
			}
		}

		public IEnumerable<OptionElement> Options
		{
			get
			{
				if (_options == null)
				{
					var optionElements = Element.Descendants()
						.Where(e => e.Name.LocalName.ToLower() == "option")
						.Select(e => this.OwningBrowser.CreateHtmlElement<OptionElement>(e));
					_options = optionElements;
				}
				return _options;
			}
		}

		/// <summary>
		/// IsSelected implements the logic of the Selected property of the underlying option elements. As the other 
		/// options are relevant in the, this has to be evaluated on the Select elements level
		/// </summary>
		/// <param name="optionElement"></param>
		/// <returns></returns>
		internal bool IsSelected(OptionElement optionElement)
		{
			if (this.MultiValued || Options.Any(o => o.GetAttributeValue("selected") != null))
			{
				return optionElement.GetAttributeValue("selected") != null;
			}
			else
			{
				return optionElement.Element == this.Options.First().Element;
			}
		}
		internal void MakeSelected(OptionElement optionElement, bool selected)
		{
			if (!selected)
			{
				optionElement.Element.RemoveAttributeCI("selected");
			}
			else
			{
				optionElement.Element.SetAttributeValue(XName.Get("selected"), "selected");
				if (!this.MultiValued)
				{
					foreach (var option in Options)
					{
						if (option.Element != optionElement.Element)
						{
							option.Element.RemoveAttributeCI("selected");
						}
					}
				}
			}
		}
		public override IEnumerable<UserVariableEntry> ValuesToSubmit(bool isClickedElement)
		{
			if (!String.IsNullOrEmpty(this.Name))
			{
				foreach (var item in this.Options)
				{
					if (item.Selected)
					{
						yield return new UserVariableEntry() { Name = this.Name, Value = item.OptionValue };
					}
				}
			}
			yield break;
		}
	}

	internal class OptionElement : HtmlElement
	{
		private SelectElement _owner = null;

		public OptionElement(XElement element)
			: base(element)
		{
		}

		public string OptionValue
		{
			get
			{
				var attr = GetAttribute("value");
				if (attr == null)
				{
					return this.Element.Value.Trim();
				}

				return attr.Value.Trim();
			}
		}

		public override string Value
		{
			get
			{
				return this.Element.Value.Trim();
			}

			set
			{
				throw new InvalidOperationException("Cannot change the value for an option element. Set the value attibute.");
			}
		}

		public SelectElement Owner
		{
			get
			{
				if (_owner == null)
				{
					var selectElement = Element.Ancestors().First(e => e.Name.LocalName.ToLower() == "select");
					_owner = this.OwningBrowser.CreateHtmlElement<SelectElement>(selectElement);
				}

				return _owner;
			}
		}

		public override bool Selected
		{
			get
			{
				// Being selected is more complicated than it seems. If a selectbox is single-valued,
				// the first option is selected when none of the options has a selected-attribute. The
				// selected state is therefor managed at the selectbox level
				return this.Owner.IsSelected(this);
			}

			set
			{
				this.Owner.MakeSelected(this, value);
			}
		}

		public override ClickResult Click()
		{
			base.Click();
			this.Selected = !this.Selected;
			return ClickResult.SucceededNoNavigation;
		}
	}
}
