// This will be inserted into the HTML - individual agent controls

        // Store original values
        const originalWidths = {};
        const originalNodeSizes = {};
        const originalCurvatures = {};
        edges.forEach(edge => {
            originalWidths[edge.id] = edge.width;
            originalCurvatures[edge.id] = edge.smooth ? edge.smooth.roundness : 0.5;
        });
        nodes.forEach(node => {
            originalNodeSizes[node.id] = node.size;
        });
        
        // Agent-specific update functions
        function updateAgentNodeSize(agentId, multiplier) {
            if (originalNodeSizes[agentId] !== undefined) {
                const newSize = originalNodeSizes[agentId] * multiplier;
                nodes.update({ id: agentId, size: newSize });
            }
        }
        
        function updateAgentEdgeThickness(agentId, edgeType, value) {
            edges.forEach(edge => {
                if (edge.sourceAgent === agentId && edge.edgeType === edgeType) {
                    const newWidth = originalWidths[edge.id] * (value / 2.0);
                    edges.update({ id: edge.id, width: newWidth });
                }
            });
        }
        
        function updateAgentEdgeCurvature(agentId, roundness) {
            edges.forEach(edge => {
                if (edge.sourceAgent === agentId) {
                    const currentSmooth = edge.smooth || { type: 'continuous', roundness: 0.5 };
                    edges.update({ 
                        id: edge.id, 
                        smooth: { 
                            type: currentSmooth.type, 
                            roundness: roundness 
                        } 
                    });
                }
            });
        }
        
        function updateAgentEdgeType(agentId, type) {
            const roundness = parseFloat(document.getElementById(`curvature_${agentId}`).value);
            edges.forEach(edge => {
                if (edge.sourceAgent === agentId) {
                    let smoothConfig;
                    if (type === 'straight') {
                        smoothConfig = false;
                    } else {
                        smoothConfig = { type: type, roundness: roundness };
                    }
                    edges.update({ id: edge.id, smooth: smoothConfig });
                }
            });
        }
        
        // Set up controls for each agent
        const agents = ['Boss', 'Tank', 'Healer', 'MeleeDPS', 'RangedDPS'];
        
        agents.forEach(agentId => {
            // Node size
            const nodeSizeSlider = document.getElementById(`nodeSize_${agentId}`);
            const nodeSizeValue = document.getElementById(`nodeSizeValue_${agentId}`);
            if (nodeSizeSlider && nodeSizeValue) {
                nodeSizeSlider.addEventListener('input', function(e) {
                    const value = parseFloat(e.target.value);
                    nodeSizeValue.textContent = value.toFixed(1);
                    updateAgentNodeSize(agentId, value);
                });
            }
            
            // Damage thickness
            const damageSlider = document.getElementById(`damageThickness_${agentId}`);
            const damageValue = document.getElementById(`damageValue_${agentId}`);
            if (damageSlider && damageValue) {
                damageSlider.addEventListener('input', function(e) {
                    const value = parseFloat(e.target.value);
                    damageValue.textContent = value.toFixed(1);
                    updateAgentEdgeThickness(agentId, 'damage', value);
                });
            }
            
            // Threat thickness (for Tank, Healer, MeleeDPS, RangedDPS)
            if (agentId !== 'Boss') {
                const threatSlider = document.getElementById(`threatThickness_${agentId}`);
                const threatValue = document.getElementById(`threatValue_${agentId}`);
                if (threatSlider && threatValue) {
                    threatSlider.addEventListener('input', function(e) {
                        const value = parseFloat(e.target.value);
                        threatValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'threat', value);
                    });
                }
            }
            
            // Taunt thickness (for Tank only)
            if (agentId === 'Tank') {
                const tauntSlider = document.getElementById(`tauntThickness_${agentId}`);
                const tauntValue = document.getElementById(`tauntValue_${agentId}`);
                if (tauntSlider && tauntValue) {
                    tauntSlider.addEventListener('input', function(e) {
                        const value = parseFloat(e.target.value);
                        tauntValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'taunt', value);
                    });
                }
            }
            
            // Healing thickness (for Healer only)
            if (agentId === 'Healer') {
                const healSlider = document.getElementById(`healThickness_${agentId}`);
                const healValue = document.getElementById(`healValue_${agentId}`);
                if (healSlider && healValue) {
                    healSlider.addEventListener('input', function(e) {
                        const value = parseFloat(e.target.value);
                        healValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'heal', value);
                    });
                }
            }
            
            // Edge curvature
            const curvatureSlider = document.getElementById(`curvature_${agentId}`);
            const curvatureValue = document.getElementById(`curvatureValue_${agentId}`);
            if (curvatureSlider && curvatureValue) {
                curvatureSlider.addEventListener('input', function(e) {
                    const value = parseFloat(e.target.value);
                    curvatureValue.textContent = value.toFixed(2);
                    updateAgentEdgeCurvature(agentId, value);
                });
            }
            
            // Edge type
            const edgeTypeSelect = document.getElementById(`edgeType_${agentId}`);
            if (edgeTypeSelect) {
                edgeTypeSelect.addEventListener('change', function(e) {
                    updateAgentEdgeType(agentId, e.target.value);
                });
            }
        });
        
        // Global arrow controls
        function updateArrowSize(lengthMultiplier, widthMultiplier) {
            const newOptions = {
                edges: {
                    arrows: {
                        to: {
                            enabled: true,
                            scaleFactor: 1.2 * lengthMultiplier,
                            type: 'arrow'
                        }
                    }
                }
            };
            network.setOptions(newOptions);
        }
        
        document.getElementById('arrowLength').addEventListener('input', function(e) {
            const lengthValue = parseFloat(e.target.value);
            const widthValue = parseFloat(document.getElementById('arrowWidth').value);
            document.getElementById('arrowLengthValue').textContent = lengthValue.toFixed(1);
            updateArrowSize(lengthValue, widthValue);
        });
        
        document.getElementById('arrowWidth').addEventListener('input', function(e) {
            const lengthValue = parseFloat(document.getElementById('arrowLength').value);
            const widthValue = parseFloat(e.target.value);
            document.getElementById('arrowWidthValue').textContent = widthValue.toFixed(1);
            updateArrowSize(lengthValue, widthValue);
        });
        
        function resetLayout() {
            nodes.forEach(node => {
                const pos = {json.dumps({n: pos[n] for n in G.nodes()}, indent=16)};
                if (pos[node.id]) {
                    nodes.update({
                        id: node.id,
                        x: pos[node.id][0] * 200,
                        y: pos[node.id][1] * 200
                    });
                }
            });
        }
        
        function resetAll() {
            resetLayout();
            // Reset all node sizes
            agents.forEach(agentId => {
                nodes.update({ id: agentId, size: originalNodeSizes[agentId] });
                const nodeSizeSlider = document.getElementById(`nodeSize_${agentId}`);
                const nodeSizeValue = document.getElementById(`nodeSizeValue_${agentId}`);
                if (nodeSizeSlider && nodeSizeValue) {
                    nodeSizeSlider.value = 1;
                    nodeSizeValue.textContent = '1.0';
                }
            });
            
            // Reset all edge thicknesses
            edges.forEach(edge => {
                edges.update({ id: edge.id, width: originalWidths[edge.id] });
            });
            
            // Reset all sliders to defaults
            agents.forEach(agentId => {
                // Damage
                const damageSlider = document.getElementById(`damageThickness_${agentId}`);
                const damageValue = document.getElementById(`damageValue_${agentId}`);
                if (damageSlider && damageValue) {
                    damageSlider.value = 2;
                    damageValue.textContent = '2';
                }
                
                // Threat
                if (agentId !== 'Boss') {
                    const threatSlider = document.getElementById(`threatThickness_${agentId}`);
                    const threatValue = document.getElementById(`threatValue_${agentId}`);
                    if (threatSlider && threatValue) {
                        threatSlider.value = 2;
                        threatValue.textContent = '2';
                    }
                }
                
                // Taunt (Tank only)
                if (agentId === 'Tank') {
                    const tauntSlider = document.getElementById(`tauntThickness_${agentId}`);
                    const tauntValue = document.getElementById(`tauntValue_${agentId}`);
                    if (tauntSlider && tauntValue) {
                        tauntSlider.value = 4;
                        tauntValue.textContent = '4';
                    }
                }
                
                // Healing (Healer only)
                if (agentId === 'Healer') {
                    const healSlider = document.getElementById(`healThickness_${agentId}`);
                    const healValue = document.getElementById(`healValue_${agentId}`);
                    if (healSlider && healValue) {
                        healSlider.value = 2;
                        healValue.textContent = '2';
                    }
                }
                
                // Curvature
                const curvatureSlider = document.getElementById(`curvature_${agentId}`);
                const curvatureValue = document.getElementById(`curvatureValue_${agentId}`);
                if (curvatureSlider && curvatureValue) {
                    curvatureSlider.value = 0.5;
                    curvatureValue.textContent = '0.5';
                    updateAgentEdgeCurvature(agentId, 0.5);
                }
                
                // Edge type
                const edgeTypeSelect = document.getElementById(`edgeType_${agentId}`);
                if (edgeTypeSelect) {
                    edgeTypeSelect.value = 'continuous';
                    updateAgentEdgeType(agentId, 'continuous');
                }
            });
            
            // Reset arrow size
            document.getElementById('arrowLength').value = 1.2;
            document.getElementById('arrowWidth').value = 1;
            document.getElementById('arrowLengthValue').textContent = '1.2';
            document.getElementById('arrowWidthValue').textContent = '1.0';
            updateArrowSize(1.2, 1.0);
        }



