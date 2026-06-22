// Testes dos helpers das Demonstrações: equilíbrio (fechado), déficit/superávit e a
// normalização dos 4 demonstrativos em quadros com total/equilíbrio.
import { describe, it, expect } from 'vitest';
import {
  abaPorSlug,
  ehDeficit,
  fechado,
  quadrosBalancoOrcamentario,
  quadrosBalancoPatrimonial,
  quadrosVariacoesPatrimoniais,
  rotuloResultado,
} from './demonstracoes.helpers';
import type {
  BalancoOrcamentario,
  BalancoPatrimonial,
  VariacoesPatrimoniais,
} from './demonstracoes.api';

describe('demonstracoes.helpers', () => {
  it('fechado() tolera diferença de até meio centavo', () => {
    expect(fechado(100, 100.004)).toBe(true);
    expect(fechado(100, 100.01)).toBe(false);
  });

  it('ehDeficit() só é verdadeiro abaixo da tolerância negativa', () => {
    expect(ehDeficit(-0.5)).toBe(true);
    expect(ehDeficit(0)).toBe(false);
    expect(ehDeficit(10)).toBe(false);
  });

  it('rotuloResultado() escolhe superávit ou déficit pelo sinal', () => {
    expect(rotuloResultado(200, 'orçamentário')).toBe('Superávit orçamentário');
    expect(rotuloResultado(-200, 'orçamentário')).toBe('Déficit orçamentário');
  });

  it('abaPorSlug() resolve a aba e cai na primeira para slug desconhecido', () => {
    expect(abaPorSlug('balanco-patrimonial').sigla).toBe('BP');
    expect(abaPorSlug('inexistente').sigla).toBe('BO');
  });

  it('normaliza o Balanço Orçamentário com totais e resultado', () => {
    const bo: BalancoOrcamentario = {
      exercicio: 2026,
      mes: 6,
      receitas: { quadro: 'Receitas', colunas: ['Realizada'], linhas: [] },
      despesas: { quadro: 'Despesas', colunas: ['Empenhada'], linhas: [] },
      totalReceitaRealizada: 1000,
      totalDespesaEmpenhada: 800,
      resultadoOrcamentario: 200,
    };
    const quadros = quadrosBalancoOrcamentario(bo);
    expect(quadros).toHaveLength(3);
    expect(quadros[0].total?.valores[0]).toBe(1000);
    expect(quadros[2].total?.linha).toBe('Superávit orçamentário');
  });

  it('normaliza o Balanço Patrimonial expondo o superávit financeiro', () => {
    const bp: BalancoPatrimonial = {
      exercicio: 2026,
      mes: 6,
      ativo: [],
      passivoPatrimonioLiquido: [],
      totalAtivo: 500,
      totalPassivoPl: 500,
      ativoFinanceiro: 500,
      passivoFinanceiro: 100,
      superavitFinanceiro: 400,
    };
    const quadros = quadrosBalancoPatrimonial(bp);
    expect(quadros[0].total?.valores[0]).toBe(500);
    expect(quadros[2].total?.linha).toBe('Superávit financeiro');
  });

  it('normaliza a DVP expondo o resultado patrimonial', () => {
    const dvp: VariacoesPatrimoniais = {
      exercicio: 2026,
      mes: 6,
      variacoesAumentativas: [],
      variacoesDiminutivas: [],
      totalVpa: 900,
      totalVpd: 1000,
      resultadoPatrimonial: -100,
    };
    const quadros = quadrosVariacoesPatrimoniais(dvp);
    expect(quadros[2].total?.linha).toBe('Déficit patrimonial');
    expect(quadros[2].total?.valores[0]).toBe(-100);
  });
});
