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

            mermaidCode += '}\n'
        });

        relationData.forEach(relation => {
            const source = relation.Source.includes('#') ? relation.Source.split('#').pop() : relation.Source.split('/').pop();
            const destination = relation.Destination.includes('#') ? relation.Destination.split('#').pop() : relation.Destination.split('/').pop();
            const type = relation.Type === 'Herencia' ? mermaidCode += `${source} --|> ${destination}\n` : mermaidCode += `${source} --> ${destination}\n`;

        });

        var mermaidContainer = document.getElementById("mermaid-container");
        mermaidContainer.innerHTML = `<pre class="mermaid">${mermaidCode}</pre>`;

        mermaid.contentLoaded();
    }
});