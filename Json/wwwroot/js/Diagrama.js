import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs";

const config = {
    startOnLoad: true,
    securityLevel: 'loose',
    themeVariables: {
        fontSize: '12px',
        nodeSpacing: 50,
        edgeLength: 100,
        arrowMarkerAbsolute: true,
        diagramPadding: 50,
    },
};

mermaid.initialize(config);

document.addEventListener("DOMContentLoaded", function () {
    if (typeof jsonData !== 'undefined') {
        const container = document.getElementById("mermaid-container");
        const downloadButton = document.getElementById("download-diagram");

        const classData = jsonData.Classes;
        const propertyData = jsonData.Properties;
        const relationData = jsonData.Relationships;

        const classProperties = {};

        classData.forEach(item => {
            const className = item.ClassName.includes('#') ? item.ClassName.split('#').pop() : item.ClassName.split('/').pop();
            classProperties[className] = [];
        });

        propertyData.forEach(property => {
            const propertyName = property.PropertyName.includes('#') ? property.PropertyName.split('#').pop() : property.PropertyName.split('/').pop();
            const domain = property.Domain.includes('#') ? property.Domain.split('#').pop() : property.Domain.split('/').pop();

            if (classProperties[domain]) {
                classProperties[domain].push({
                    PropertyName: propertyName,
                    Cardinality: property.Cardinality,
                    Range: property.Range.includes('#') ? property.Range.split('#').pop() : property.Range.split('/').pop(),
                });
            }
        });

        var mermaidCode = 'classDiagram\n';
        mermaidCode += 'direction LR\n';

        Object.keys(classProperties).forEach(className => {
            mermaidCode += `class ${className} {\n`;

            classProperties[className].forEach(property => {
                mermaidCode += `    ${property.PropertyName}[${property.Cardinality}]: ${property.Range}\n`;
            });

            mermaidCode += '}\n';
        });

        relationData.forEach(relation => {
            const source = relation.Source.includes('#') ? relation.Source.split('#').pop() : relation.Source.split('/').pop();
            const destination = relation.Destination.includes('#') ? relation.Destination.split('#').pop() : relation.Destination.split('/').pop();

            if (relation.Type === 'Herencia') {
                mermaidCode += `${source} --|> ${destination}\n`;
            } else {
                mermaidCode += `${source} --> ${destination}\n`;
            }
        });

        console.log("Código Mermaid generado:\n", mermaidCode);

        // Insertar Mermaid en el contenedor
        container.innerHTML = `<pre class="mermaid">${mermaidCode}</pre>`;

        // Forzar renderizado de Mermaid
        mermaid.contentLoaded();

        const modal = document.createElement("div");
        modal.style.position = "fixed";
        modal.style.top = "50%";
        modal.style.left = "50%";
        modal.style.transform = "translate(-50%, -50%)";
        modal.style.backgroundColor = "white";
        modal.style.padding = "20px";
        modal.style.border = "1px solid #ccc";
        modal.style.boxShadow = "0 0 10px rgba(0, 0, 0, 0.1)";
        modal.style.zIndex = "1000";
        modal.style.display = "none"; // Ocultar inicialmente
        document.body.appendChild(modal);

        // Agregar listener de doble clic a las clases
        setTimeout(() => {
            const classElements = container.querySelectorAll('g.node.default');
            classElements.forEach(classElement => {
                classElement.addEventListener('dblclick', () => {
                    // Seleccionar el título de la clase
                    const classTitleElement = classElement.querySelector('foreignObject.classTitle span.nodeLabel');
                    if (classTitleElement) {
                        const className = classTitleElement.textContent.trim();
                        const properties = classProperties[className];

                        // Mostrar el modal con el título y los atributos
                        modal.innerHTML = `
                            <h3>Editar clase: ${className}</h3>
                            <div>
                                <label>Nombre de la clase:</label>
                                <input type="text" id="class-name" value="${className}" />
                            </div>
                            <div id="attributes-container">
                                <h4>Propiedades:</h4>
                            </div>
                            <button id="add-property">Agregar propiedad</button>
                            <button id="save-changes">Guardar cambios</button>
                            <button id="close-modal">Cerrar</button>
                        `;

                        const classNameInput = modal.querySelector("#class-name");
                        const attributesContainer = modal.querySelector("#attributes-container");

                        // Mostrar las propiedades existentes
                        if (properties) {
                            properties.forEach((prop, index) => {
                                attributesContainer.innerHTML += `
                                    <div>
                                        <label>Nombre:</label>
                                        <input type="text" value="${prop.PropertyName}" data-index="${index}" data-field="PropertyName" />
                                        <label>Cardinalidad:</label>
                                        <input type="text" value="${prop.Cardinality}" data-index="${index}" data-field="Cardinality" />
                                        <label>Rango:</label>
                                        <input type="text" value="${prop.Range}" data-index="${index}" data-field="Range" />
                                        <button class="delete-property" data-index="${index}">Eliminar</button>
                                    </div>
                                `;
                            });
                        }

                        // Agregar nueva propiedad
                        const addPropertyButton = modal.querySelector("#add-property");
                        addPropertyButton.addEventListener("click", () => {
                            const newProperty = {
                                PropertyName: "NuevaPropiedad",
                                Cardinality: "1",
                                Range: "string",
                            };
                            properties.push(newProperty);

                            // Actualizar el modal con la nueva propiedad
                            attributesContainer.innerHTML += `
                                <div>
                                    <label>Nombre:</label>
                                    <input type="text" value="${newProperty.PropertyName}" data-index="${properties.length - 1}" data-field="PropertyName" />
                                    <label>Cardinalidad:</label>
                                    <input type="text" value="${newProperty.Cardinality}" data-index="${properties.length - 1}" data-field="Cardinality" />
                                    <label>Rango:</label>
                                    <input type="text" value="${newProperty.Range}" data-index="${properties.length - 1}" data-field="Range" />
                                    <button class="delete-property" data-index="${properties.length - 1}">Eliminar</button>
                                </div>
                            `;
                        });

                        // Eliminar propiedad
                        attributesContainer.addEventListener("click", (e) => {
                            if (e.target.classList.contains("delete-property")) {
                                const index = e.target.getAttribute("data-index");
                                properties.splice(index, 1); // Eliminar la propiedad
                                modal.style.display = "none"; // Cerrar y volver a abrir el modal para refrescar
                                classElement.dispatchEvent(new Event('dblclick')); // Simular doble clic
                            }
                        });

                        // Guardar cambios
                        const saveButton = modal.querySelector("#save-changes");
                        saveButton.addEventListener("click", () => {
                            // Actualizar el nombre de la clase
                            const newClassName = classNameInput.value.trim();
                            if (newClassName && newClassName !== className) {
                                classProperties[newClassName] = classProperties[className];
                                delete classProperties[className];
                                classTitleElement.textContent = newClassName;
                            }

                            // Actualizar las propiedades
                            const inputs = attributesContainer.querySelectorAll("input");
                            inputs.forEach(input => {
                                const index = input.getAttribute("data-index");
                                const field = input.getAttribute("data-field");
                                if (index !== null && properties[index]) {
                                    properties[index][field] = input.value; // Actualizar el valor
                                }
                            });

                            modal.style.display = "none"; // Ocultar el modal
                            mermaid.contentLoaded();
                        });

                        // Cerrar el modal
                        const closeButton = modal.querySelector("#close-modal");
                        closeButton.addEventListener("click", () => {
                            modal.style.display = "none"; // Ocultar el modal
                        });

                        // Mostrar el modal
                        modal.style.display = "block";
                    } else {
                        console.error("No se pudo encontrar el título de la clase.");
                    }
                });
            });
        }, 1000);
        downloadButton.addEventListener("click", function () {
            const svgElement = container.querySelector("svg");
            if (!svgElement) {
                alert("El diagrama aún no está disponible.");
                return;
            }

            // Convertir SVG a XML
            const serializer = new XMLSerializer();
            let source = serializer.serializeToString(svgElement);

            // Crear un Blob con los datos
            const blob = new Blob([source], { type: "image/svg+xml;charset=utf-8" });
            const url = URL.createObjectURL(blob);

            // Crear un enlace para descargar
            const a = document.createElement("a");
            a.href = url;
            a.download = "diagrama.svg";
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);

            // Liberar URL
            URL.revokeObjectURL(url);
        });
    }
});
