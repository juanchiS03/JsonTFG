using Json.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Xml.Linq;

namespace Json.Controllers
{
    public class OwlToJsonController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ConvertOwlToJson(IFormFile owlFile)
        {
            if (owlFile == null || owlFile.Length == 0)
                return BadRequest("No file uploaded.");

            // Procesar el archivo OWL
            var model = ProcessOwlFile(owlFile);

            // Convertir el modelo a JSON
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            // Devolver el JSON
            //return Content(json, "application/json");

            // Guardar el JSON en TempData
            TempData["jsonData"] = json;

            // Redirigir a la vista para mostrar el diagrama
            return RedirectToAction("VistaGraf", "Uml");
        }

        private OwlToJsonModel ProcessOwlFile(IFormFile owlFile)
        {
            var model = new OwlToJsonModel();
            var namespaces = new Dictionary<string, XNamespace>();

            using (var stream = owlFile.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                var owlContent = reader.ReadToEnd();
                var xmlDocument = XDocument.Parse(owlContent);

                // Extraer namespaces de xmlns: en <rdf:RDF>
                foreach (var attr in xmlDocument.Root.Attributes().Where(a => a.IsNamespaceDeclaration)) 
                {
                    var prefix = attr.Name.LocalName;
                    var uri = attr.Value;
                    namespaces[prefix] = uri;
                }

                // Obtener rdf:about dinámicamente
                if (!namespaces.TryGetValue("rdf", out XNamespace rdf))
                {
                    rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
                }

                // Buscar Clases
                foreach (var classElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "Class"))
                {
                    var className = classElement.Attribute(rdf + "about")?.Value;
                    var label = classElement.Elements().FirstOrDefault(e => e.Name.LocalName == "label")?.Value;

                    if (!string.IsNullOrEmpty(className))
                    {
                        model.Classes.Add(new OwlClass
                        {
                            ClassName = className,
                            Label = label
                        });
                    }
                }

                // Buscar todas las subclases de una clase base
                var allSubclasses = new Dictionary<string, HashSet<string>>();

                // Buscar todas las clases y sus subclases
                foreach (var classElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "Class"))
                {
                    var className = classElement.Attribute(rdf + "about")?.Value;

                    if (!string.IsNullOrEmpty(className))
                    {
                        foreach (var subClassElement in classElement.Elements().Where(e => e.Name.LocalName == "subClassOf"))
                        {
                            var resource = subClassElement.Attribute(rdf + "resource")?.Value; 
                            var nodeID = subClassElement.Attribute(rdf + "nodeID")?.Value; 

                            if (!string.IsNullOrEmpty(resource))
                            {
                                if (!allSubclasses.ContainsKey(resource))
                                {
                                    allSubclasses[resource] = new HashSet<string>();
                                }
                                allSubclasses[resource].Add(className);
                            }

                            if (!string.IsNullOrEmpty(nodeID))
                            {
                                if (!allSubclasses.ContainsKey(nodeID))
                                {
                                    allSubclasses[nodeID] = new HashSet<string>();
                                }
                                allSubclasses[nodeID].Add(className);
                            }
                        }
                    }
                }

                // Buscar subclases en Description nodeID
                foreach (var classAuto in xmlDocument.Descendants().Where(e => e.Name.LocalName == "Description"))
                {
                    var nodeID = classAuto.Attribute(rdf + "nodeID")?.Value;
                    if (nodeID != null)
                    {
                        var union = classAuto.Descendants().FirstOrDefault(e => e.Name.LocalName == "unionOf");
                        if (union != null)
                        {
                            var firstElements = union.Descendants().Where(e => e.Name.LocalName == "first");

                            foreach (var first in firstElements)
                            {
                                var resource = first.Attribute(rdf + "resource")?.Value;
                                if (resource != null)
                                {
                                    if (!allSubclasses.ContainsKey(nodeID))
                                    {
                                        allSubclasses[nodeID] = new HashSet<string>();
                                    }
                                    allSubclasses[nodeID].Add(resource);
                                }
                            }
                        }
                    }
                }

                // Buscar Propiedades
                foreach (var propertyElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "DatatypeProperty" || e.Name.LocalName == "ObjectProperty" || e.Name.LocalName == "FunctionalProperty"))
                {
                    var propertyName = propertyElement.Attribute(rdf + "about")?.Value;
                    var label = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "label")?.Value;
                    var comment = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "comment")?.Value;
                    var domain = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "domain")?.Attribute(rdf + "resource")?.Value ?? propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "domain")?.Attribute(rdf + "nodeID")?.Value;
                    var range = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "range")?.Attribute(rdf + "resource")?.Value;

                    if (domain.Contains("autos"))
                    {
                        if (allSubclasses.ContainsKey(domain))
                        {
                            var subClasses = allSubclasses.GetValueOrDefault(domain);
                            foreach (var subClass in subClasses)
                            {
                                if (!string.IsNullOrEmpty(propertyName))
                                {
                                    model.Properties.Add(new OwlProperty
                                    {
                                        PropertyName = propertyName,
                                        Label = label,
                                        Comment = comment,
                                        Domain = subClass,
                                        Range = range
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            model.Properties.Add(new OwlProperty
                            {
                                PropertyName = propertyName,
                                Label = label,
                                Comment = comment,
                                Domain = domain,
                                Range = range
                            });
                        }
                    }              
                }

                foreach (var restrictionElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "Restriction"))
                {
                    var nodeID = restrictionElement.Attribute(rdf + "nodeID")?.Value;

                    var cardinalityElement = restrictionElement.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "cardinality");

                    string cardinality = null;

                    if (cardinalityElement != null)
                    {
                        cardinality = cardinalityElement.Value;
                    }
                    else
                    {
                        // Si no hay cardinalidad exacta, buscamos min y max cardinality
                        var minCardinalityElement = restrictionElement.Elements()
                            .FirstOrDefault(e => e.Name.LocalName == "minCardinality");
                        var maxCardinalityElement = restrictionElement.Elements()
                            .FirstOrDefault(e => e.Name.LocalName == "maxCardinality");

                        if (minCardinalityElement != null && maxCardinalityElement != null)
                        {
                            // Si ambos existen, retornamos un rango
                            cardinality = $"{minCardinalityElement.Value}..{maxCardinalityElement.Value}";
                        }
                        else if (minCardinalityElement != null)
                        {
                            cardinality = $"{minCardinalityElement.Value}..*"; 
                        }
                        else if (maxCardinalityElement != null)
                        {
                            cardinality = $"0..{maxCardinalityElement.Value}";
                        }
                        else
                        {
                            cardinality = "*";
                        }
                    }

                    var onProperty = restrictionElement.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "onProperty")?.Attribute(rdf + "resource")?.Value;

                    var property = model.Properties.FirstOrDefault(p => p.PropertyName == onProperty);
                    if (property != null)
                    {
                        // Verificar si el dominio de la propiedad es una subclase o igual al nodeID
                        if ((allSubclasses.ContainsKey(nodeID) && allSubclasses[nodeID].Contains(property.Domain)) || property.Domain.Equals(nodeID))
                        {
                            property.Cardinality = cardinality;
                        }
                    }
                }

                // Asignar '*' a las propiedades sin cardinalidad
                foreach (var prop in model.Properties)
                {
                    if (string.IsNullOrEmpty(prop.Cardinality))
                    {
                        prop.Cardinality = "*";
                    }
                }

                // Añadir relaciones de herencia
                foreach (var clase in model.Classes)
                {
                    if (allSubclasses.ContainsKey(clase.ClassName))
                    {
                        foreach (var subClase in allSubclasses[clase.ClassName])
                        {
                            model.Relationships.Add(new OwlRelationship
                            {
                                Type = OwlRelationshipType.Herencia,
                                Destination = clase.ClassName,
                                Source = subClase
                            });
                        }
                    }
                }

                // Añadir resto de asociaciones
                foreach (var prop in model.Properties)
                {
                    var inic = prop.Domain;
                    var fin = prop.Range;

                    if (inic != null && fin != null)
                    {
                        if (IsClass(fin))
                        {
                            model.Relationships.Add(new OwlRelationship
                            {
                                Source = inic,
                                Destination = fin,
                                Predicate = prop.PropertyName,
                                Type = OwlRelationshipType.Asociacion
                            });
                        }
                    }
                }

                bool IsClass(string value)
                {
                    return !value.StartsWith("http://www.w3.org/2001/XMLSchema#");
                }

            }

            return model;
        }
    }
}