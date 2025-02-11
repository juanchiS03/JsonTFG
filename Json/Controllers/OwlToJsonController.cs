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

            // Devolver el JSON como una respuesta
            return Content(json, "application/json");
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

                // Buscar Propiedades
                foreach (var propertyElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "DatatypeProperty" || e.Name.LocalName == "ObjectProperty" || e.Name.LocalName == "FunctionalProperty"))
                {
                    var propertyName = propertyElement.Attribute(rdf + "about")?.Value;
                    var label = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "label")?.Value;
                    var comment = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "comment")?.Value;
                    var domain = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "domain")?.Attribute(rdf + "resource")?.Value ?? propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "domain")?.Attribute(rdf + "nodeID")?.Value;
                    var range = propertyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "range")?.Attribute(rdf + "resource")?.Value;


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

                // Buscar todas las subclases de una clase base
                // Diccionario para almacenar las subclases de una clase base (por rdf:resource y nodeID)
                var allSubclasses = new Dictionary<string, HashSet<string>>(); // Clase base -> Subclases

                // Buscar todas las clases y sus subclases (por rdf:resource y nodeID)
                foreach (var classElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "Class"))
                {
                    var className = classElement.Attribute(rdf + "about")?.Value;

                    if (!string.IsNullOrEmpty(className))
                    {
                        // Buscar las subclases de esta clase en las relaciones rdfs:subClassOf
                        foreach (var subClassElement in classElement.Elements().Where(e => e.Name.LocalName == "subClassOf"))
                        {
                            var resource = subClassElement.Attribute(rdf + "resource")?.Value; // Subclase por rdf:resource
                            var nodeID = subClassElement.Attribute(rdf + "nodeID")?.Value;   // Subclase por nodeID

                            // Si tiene un rdf:resource, es una subclase de la clase actual
                            if (!string.IsNullOrEmpty(resource))
                            {
                                if (!allSubclasses.ContainsKey(resource))
                                {
                                    allSubclasses[resource] = new HashSet<string>();
                                }
                                allSubclasses[resource].Add(className); // Agregar la subclase
                            }

                            // Si tiene un nodeID, es una subclase identificada de forma local
                            if (!string.IsNullOrEmpty(nodeID))
                            {
                                if (!allSubclasses.ContainsKey(nodeID))
                                {
                                    allSubclasses[nodeID] = new HashSet<string>();
                                }
                                allSubclasses[nodeID].Add(className); // Agregar la subclase
                            }
                        }
                    }
                }

                // Crear un diccionario para buscar rápidamente las propiedades por 'onProperty'
                var propertiesByName = model.Properties.ToDictionary(p => p.PropertyName);

                // Añadir restricciones
                foreach (var restrictionElement in xmlDocument.Descendants().Where(e => e.Name.LocalName == "Restriction"))
                {
                    var nodeID = restrictionElement.Attribute(rdf + "nodeID")?.Value;

                    // Almacenar los elementos de cardinalidad en una sola pasada
                    var cardinalityElement = restrictionElement.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "cardinality") ??
                        restrictionElement.Elements().FirstOrDefault(e => e.Name.LocalName == "maxCardinality") ??
                        restrictionElement.Elements().FirstOrDefault(e => e.Name.LocalName == "minCardinality");

                    var cardinality = cardinalityElement?.Value;
                    var onProperty = restrictionElement.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "onProperty")?.Attribute(rdf + "resource")?.Value;

                    // Verificar si la propiedad existe y si el dominio de la propiedad coincide con el nodeID
                    if (onProperty != null && propertiesByName.TryGetValue(onProperty, out var prop))
                    {
                        // Verificar si el dominio de la propiedad es una subclase de nodeID o si el dominio es igual a nodeID
                        if ((allSubclasses.ContainsKey(nodeID) && allSubclasses[nodeID].Contains(prop.Domain)) || prop.Domain.Equals(nodeID))
                        {
                            prop.Cardinality = cardinality; // Asignar la cardinalidad
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





                // Buscar Relaciones entre Clases
                foreach (var prop in model.Properties)
                {
                    var origen = prop.Domain;
                    var destino = prop.Range;

                    // Verificar si el origen y el destino son clases (no tipos de datos)
                    if (IsClass(origen) && IsClass(destino))
                    {
                        // Aquí tienes una relación entre clases
                        Console.WriteLine($"Relación encontrada: {prop.PropertyName}");
                        Console.WriteLine($"Dominio (origen): {origen}");
                        Console.WriteLine($"Rango (destino): {destino}");
                    }
                }

                // Método para verificar si un valor es una clase (no un tipo de datos)
                bool IsClass(string value)
                {
                    // Compara si el valor es una URI que corresponde a una clase (no es un tipo de datos)
                    return !value.StartsWith("http://www.w3.org/2001/XMLSchema#");
                }
            }

            return model;
        }

    }
}
