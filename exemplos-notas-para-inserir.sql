-- pagamento aprovado
INSERT INTO public.nota(
	descricao, datapagamento, valor, solicitante, situacao)
	VALUES ('Nota dentro do prazo de processamento', '2025-01-28', 134.50, 'Fulano', '');

-- pagamento reprovado
INSERT INTO public.nota(
	descricao, datapagamento, valor, solicitante, situacao)
	VALUES ('Nota dentro do prazo de processamento', '2025-01-18', 134.50, 'Fulano', '');
    
--pagamento aprovado pela alcada 
INSERT INTO public.nota(
	descricao, datapagamento, valor, solicitante, situacao)
	VALUES ('Nota dentro do prazo de processamento', '2025-01-22', 134.50, 'Fulano', '');

--pagamento reprovado pela alcada
INSERT INTO public.nota(
	descricao, datapagamento, valor, solicitante, situacao)
	VALUES ('Nota dentro do prazo de processamento', '2025-01-22', 2134.50, 'Fulano', '');

--pagamento aprovado pela alcada
INSERT INTO public.nota(
	descricao, datapagamento, valor, solicitante, situacao)
	VALUES ('Nota dentro do prazo de processamento', '2025-01-22', 2134.50, 'Beltrano', '');

--pagamento reprovado pela alcada
INSERT INTO public.nota(
	descricao, datapagamento, valor, solicitante, situacao)
	VALUES ('Nota dentro do prazo de processamento', '2025-01-22', 7134.50, 'Beltrano', '');
