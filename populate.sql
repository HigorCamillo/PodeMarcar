
-- Clientes master
INSERT INTO clientes_master (nome, email, telefone) VALUES
('Barbearia Teste', 'contato@barbeariateste.com', '11999999999');

-- Funcionarios (com imagem local)
INSERT INTO funcionarios (id_cliente_master, nome, imagem_url) VALUES
(1, 'Lucas', '/images/employees/lucas.png'),
(1, 'Pedro', '/images/employees/pedro.png');

-- Servicos (com imagem local)
INSERT INTO servicos (id_cliente_master, nome, preco, duracao_minutos, ativo, imagem_url) VALUES
(1, 'Barba', 25.00, 20, TRUE, '/images/services/barba.png'),
(1, 'Corte de Cabelo', 40.00, 30, TRUE, '/images/services/cabelo.png');

-- Vinculo funcionario <-> servico
INSERT INTO funcionarios_servicos (id_funcionario, id_servico) VALUES
(1, 1), -- Lucas faz Barba
(2, 2); -- Pedro faz Corte de Cabelo

-- Clientes finais
INSERT INTO clientes (id_cliente_master, nome, telefone, email) VALUES
(1, 'João da Silva', '11999998888', 'joao@gmail.com');

-- Horarios disponíveis (exemplo)
INSERT INTO horarios_disponiveis (id_cliente_master, id_funcionario, data, hora, disponivel) VALUES
(1, 1, CURRENT_DATE + INTERVAL '1 day', '09:00', TRUE),
(1, 1, CURRENT_DATE + INTERVAL '1 day', '09:30', TRUE),
(1, 2, CURRENT_DATE + INTERVAL '2 day', '14:00', TRUE);
